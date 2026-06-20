// AstroEditor/Core/Execution/TaskScheduler.cs
// Реализация планировщика многозадачности

using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroTypes = AstroEditor.Core.Types.DataTypeRegistry;

namespace AstroEditor.Core.Execution;

public class TaskScheduler : ITaskScheduler, IExecutor, IDisposable
{
    private readonly Dictionary<int, TaskState> _tasks = new();
    private readonly Dictionary<int, TaskConfig> _configs = new();
    private readonly object _lock = new();
    private Thread? _schedulerThread;
    private volatile bool _running;
    private int _nextTaskId = 1;

    public VariableTableSet GlobalTables { get; init; } = null!;
    public Dictionary<string, Func<object?[], object?>> Functions { get; init; } = new();
    public Dictionary<string, AstroProgram> ProgramRegistry { get; init; } = new();
    public AstroTypes TypeRegistry { get; init; } = null!;

    // Сервисы (для проброса в интерпретатор)
    public IAlarmService? AlarmService { get; init; }
    public IInterruptService? InterruptService { get; init; }
    public ITimerService? TimerService { get; init; }

    // События
    public event Action<TaskState, int>? OnBeforeLine;
    public event Action<TaskState, int>? OnAfterLine;
    public event Action<TaskState, Exception>? OnError;
    public event Action<TaskState>? OnTaskStarted;
    public event Action<TaskState>? OnTaskStopped;

    public TaskState StartTask(TaskConfig config)
    {
        lock (_lock)
        {
            var taskId = config.TaskId > 0 ? config.TaskId : _nextTaskId++;
            if (_tasks.ContainsKey(taskId))
                throw new InvalidOperationException($"Task {taskId} already exists");

            // Клонируем локальные таблицы для изоляции
            var localTables = new VariableTableSet
            {
                Name = $"{config.Program.Name}_Task{taskId}",
                IsGlobal = false
            };

            foreach (var kv in config.Program.LocalTables.Tables)
            {
                var srcTable = kv.Value;
                var type = TypeRegistry.GetTypeById(srcTable.TypeId);
                if (type == null) continue;

                var dstTable = localTables.GetOrCreateTable(type);
                foreach (var v in srcTable.Variables)
                    dstTable.AddVariable(v);
            }

            var state = new TaskState
            {
                TaskId = taskId,
                Name = config.Name,
                Program = config.Program,
                LocalTables = localTables,
                CurrentLineIndex = 0,
                IsRunning = true
            };

            _tasks[taskId] = state;
            _configs[taskId] = config;

            OnTaskStarted?.Invoke(state);
            return state;
        }
    }

    public void PauseTask(int taskId)
    {
        lock (_lock)
        {
            if (_tasks.TryGetValue(taskId, out var state))
                state.IsPaused = true;
        }
    }

    public void ResumeTask(int taskId)
    {
        lock (_lock)
        {
            if (_tasks.TryGetValue(taskId, out var state))
                state.IsPaused = false;
        }
    }

    public void StopTask(int taskId)
    {
        lock (_lock)
        {
            if (_tasks.TryGetValue(taskId, out var state))
            {
                state.StopRequested = true;
                state.IsRunning = false;
                OnTaskStopped?.Invoke(state);
                _tasks.Remove(taskId);
                _configs.Remove(taskId);
            }
        }
    }

    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var id in _tasks.Keys.ToList())
                StopTask(id);
        }
    }

    public TaskState? GetTaskState(int taskId)
    {
        lock (_lock)
        {
            _tasks.TryGetValue(taskId, out var state);
            return state;
        }
    }

    public IReadOnlyList<TaskState> GetAllTasks()
    {
        lock (_lock)
        {
            return _tasks.Values.ToList().AsReadOnly();
        }
    }

    public void StepTask(int taskId)
    {
        lock (_lock)
        {
            if (!_tasks.TryGetValue(taskId, out var state) || state.StopRequested)
                return;

            if (state.IsPaused) return;
            ExecuteOne(state);
        }
    }

    public void StartScheduler()
    {
        if (_running) return;
        _running = true;
        _schedulerThread = new Thread(SchedulerLoop)
        {
            Name = "AST-TaskScheduler",
            IsBackground = true
        };
        _schedulerThread.Start();
    }

    public void StopScheduler()
    {
        _running = false;
        _schedulerThread?.Join(1000);
        StopAll();
    }

    private void SchedulerLoop()
    {
        while (_running)
        {
            List<TaskState> foregroundTasks;
            List<(int TaskId, TaskConfig Config)> backgroundConfigs;

            lock (_lock)
            {
                foregroundTasks = _tasks
                    .Where(kv => _configs.TryGetValue(kv.Key, out var cfg) && cfg.Type == TaskType.Foreground)
                    .Select(kv => kv.Value)
                    .Where(s => s.IsRunning && !s.IsPaused && !s.StopRequested)
                    .ToList();

                backgroundConfigs = _configs
                    .Where(kv => kv.Value.Type == TaskType.Background)
                    .Select(kv => (kv.Key, kv.Value))
                    .ToList();
            }

            foreach (var task in foregroundTasks)
            {
                lock (_lock) { ExecuteOne(task); }
                Thread.Sleep(1);
            }

            var now = DateTime.UtcNow;
            foreach (var (id, cfg) in backgroundConfigs)
            {
                lock (_lock)
                {
                    if (!_tasks.TryGetValue(id, out var state) || state.StopRequested)
                        continue;

                    if (cfg.CycleIntervalMs > 0)
                    {
                        var elapsed = (now - state.StartedAt).TotalMilliseconds;
                        if (elapsed < cfg.CycleIntervalMs) continue;
                    }

                    if (cfg.MaxCycles > 0 && state.CallStack.Count >= cfg.MaxCycles)
                        continue;

                    ExecuteOne(state);
                }
            }

            Thread.Sleep(10);
        }
    }

    public void ExecuteOne(TaskState task)
    {
        if (task.CurrentLineIndex >= task.Program.Lines.Count)
        {
            task.IsRunning = false;
            task.StopRequested = true;
            return;
        }

        var instruction = task.Program.Lines[task.CurrentLineIndex];
        OnBeforeLine?.Invoke(task, instruction.LineNumber);

        try
        {
            var interpCtx = new Interpreter.InterpreterContext
            {
                TypeRegistry = TypeRegistry,
                FormRegistry = new Forms.FormRegistry(),
                GlobalTables = GlobalTables,
                Functions = Functions,
                ProgramRegistry = ProgramRegistry,
                AlarmService = AlarmService,
                InterruptService = InterruptService,
                TimerService = TimerService
            };

            var interpreter = new Interpreter.AstroInterpreter(interpCtx);
            interpreter.LoadProgram(task.Program, task.LocalTables);
            interpreter.State.CurrentLineIndex = task.CurrentLineIndex;
            interpreter.Step();
            task.CurrentLineIndex = interpreter.State.CurrentLineIndex;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(task, ex);
            task.IsRunning = false;
            task.StopRequested = true;
        }

        OnAfterLine?.Invoke(task, instruction.LineNumber);
    }

    public void ExecuteAll(TaskState task)
    {
        while (!task.StopRequested && task.CurrentLineIndex < task.Program.Lines.Count)
        {
            lock (_lock) { ExecuteOne(task); }
            Thread.Sleep(1);
        }
    }

    public void Dispose()
    {
        StopScheduler();
    }
}