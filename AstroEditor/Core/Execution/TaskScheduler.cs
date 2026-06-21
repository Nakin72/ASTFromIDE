// AstroEditor/Core/Execution/TaskScheduler.cs
// Реализация планировщика многозадачности
// ✅ P1: Исправлен race condition через Lazy<InterpreterContext>

using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Interpreter;
using AstroTypes = AstroEditor.Core.Types.DataTypeRegistry;

namespace AstroEditor.Core.Execution;

public class TaskScheduler : ITaskScheduler, IExecutor, IDisposable
{
    private readonly Dictionary<int, TaskState> _tasks = new();
    private readonly Dictionary<int, TaskConfig> _configs = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _schedulerTask;
    private volatile bool _running;
    private int _nextTaskId = 1;

    public VariableTableSet GlobalTables { get; init; } = null!;
    public Dictionary<string, Func<object?[], object?>> Functions { get; init; } = new();
    public Dictionary<string, AstroProgram> ProgramRegistry { get; init; } = new();
    public AstroTypes TypeRegistry { get; init; } = null!;
    public Forms.FormRegistry FormRegistry { get; init; } = null!;

    // Сервисы (для проброса в интерпретатор)
    public IAlarmService? AlarmService { get; init; }
    public IInterruptService? InterruptService { get; init; }
    public ITimerService? TimerService { get; init; }
    public IBindingService? BindingService { get; init; }
    public IExpressionCache? ExpressionCache { get; init; }

    // ✅ P1: Lazy<InterpreterContext> для потокобезопасной инициализации
    private readonly Lazy<InterpreterContext> _interpreterContextLazy;
    private InterpreterContext InterpreterContext => _interpreterContextLazy.Value;
    
    // Пул интерпретаторов — по одному на задачу (для избежания создания каждый шаг)
    private readonly Dictionary<int, AstroInterpreterEx> _interpreters = new();

    // События
    public event Action<TaskState, int>? OnBeforeLine;
    public event Action<TaskState, int>? OnAfterLine;
    public event Action<TaskState, Exception>? OnError;
    public event Action<TaskState>? OnTaskStarted;
    public event Action<TaskState>? OnTaskStopped;

    public TaskScheduler()
    {
        // ✅ P1: Lazy инициализация с ExecutionAndPublication для потокобезопасности
        _interpreterContextLazy = new Lazy<InterpreterContext>(CreateInterpreterContext, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Создать планировщик с полным набором зависимостей.
    /// </summary>
    public TaskScheduler(
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programRegistry,
        AstroTypes typeRegistry,
        Forms.FormRegistry formRegistry)
    {
        GlobalTables = globalTables ?? throw new ArgumentNullException(nameof(globalTables));
        ProgramRegistry = programRegistry ?? throw new ArgumentNullException(nameof(programRegistry));
        TypeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        FormRegistry = formRegistry ?? throw new ArgumentNullException(nameof(formRegistry));
        
        // ✅ P1: Lazy инициализация контекста
        _interpreterContextLazy = new Lazy<InterpreterContext>(CreateInterpreterContext, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Создать контекст интерпретатора.
    /// Вызывается лениво при первом обращении.
    /// </summary>
    private InterpreterContext CreateInterpreterContext()
    {
        return new InterpreterContext
        {
            TypeRegistry = TypeRegistry,
            FormRegistry = FormRegistry ?? new Forms.FormRegistry(),
            GlobalTables = GlobalTables,
            Functions = Functions,
            ProgramRegistry = ProgramRegistry,
            AlarmService = AlarmService,
            InterruptService = InterruptService,
            TimerService = TimerService,
            BindingService = BindingService,
            ExpressionCache = ExpressionCache
        };
    }

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

            // ✅ P1: Используем Lazy<InterpreterContext>
            var interpreter = new AstroInterpreterEx(InterpreterContext, null, ExpressionCache);
            interpreter.LoadProgram(config.Program, localTables);
            _interpreters[taskId] = interpreter;

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
                
                // ✅ Освобождаем интерпретатор при остановке задачи
                if (_interpreters.TryGetValue(taskId, out var interpreter))
                {
                    _interpreters.Remove(taskId);
                    // Интерпретаторы не IDisposable, но сбрасываем состояние
                    interpreter.Reset();
                }
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
        _cancellationTokenSource = new CancellationTokenSource();
        
        // ✅ P2: Task.Run вместо new Thread (использует ThreadPool)
        _schedulerTask = Task.Run(() => SchedulerLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public void StopScheduler()
    {
        _running = false;
        _cancellationTokenSource?.Cancel();
        _schedulerTask?.Wait(1000);
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _schedulerTask = null;
        StopAll();
    }

    private void SchedulerLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!_cancellationTokenSource?.IsCancellationRequested == false && !cancellationToken.IsCancellationRequested)
            {
                List<TaskState> foregroundTasks;
                List<(int TaskId, TaskConfig Config)> backgroundConfigs;

                try
                {
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error collecting tasks: {ex.Message}");
                    Task.Delay(100, cancellationToken).Wait(cancellationToken);
                    continue;
                }

                foreach (var task in foregroundTasks)
                {
                    try
                    {
                        lock (_lock) { ExecuteOne(task); }
                        // ✅ P2: Task.Delay вместо Thread.Sleep
                        Task.Delay(1, cancellationToken).Wait(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Нормальное завершение при отмене
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error executing foreground task {task.TaskId}: {ex.Message}");
                        task.IsRunning = false;
                        task.StopRequested = true;
                        OnError?.Invoke(task, ex);
                    }
                }

                var now = DateTime.UtcNow;
                foreach (var (id, cfg) in backgroundConfigs)
                {
                    try
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
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error executing background task {id}: {ex.Message}");
                        lock (_lock)
                        {
                            if (_tasks.TryGetValue(id, out var state))
                            {
                                state.IsRunning = false;
                                state.StopRequested = true;
                                OnError?.Invoke(state, ex);
                            }
                        }
                    }
                }

                // ✅ P2: Task.Delay вместо Thread.Sleep
                Task.Delay(10, cancellationToken).Wait(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при отмене
            System.Diagnostics.Debug.WriteLine("Scheduler loop cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scheduler loop terminated unexpectedly: {ex.Message}");
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
            // ✅ Переиспользуем интерпретатор, созданный при старте задачи
            if (!_interpreters.TryGetValue(task.TaskId, out var interpreter))
            {
                // Fallback на случай если интерпретатор не был создан
                // ✅ P1: Используем Lazy<InterpreterContext>
                interpreter = new AstroInterpreterEx(InterpreterContext, null, ExpressionCache);
                interpreter.LoadProgram(task.Program, task.LocalTables);
                _interpreters[task.TaskId] = interpreter;
            }
            
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
            // ✅ P2: Task.Delay вместо Thread.Sleep
            Task.Delay(1).Wait();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Освободить ресурсы планировщика.
    /// ✅ P2: Улучшенная реализация IDisposable.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Останавливаем планировщик
            StopScheduler();
            
            // ✅ P2: Корректно освобождаем интерпретаторы
            lock (_lock)
            {
                foreach (var interpreter in _interpreters.Values)
                {
                    try
                    {
                        interpreter?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Логируем, но не выбрасываем в Dispose
                        System.Diagnostics.Debug.WriteLine($"Error disposing interpreter: {ex.Message}");
                    }
                }
                _interpreters.Clear();
            }
            
            // Освобождаем CancellationTokenSource
            _cancellationTokenSource?.Dispose();
        }
    }
    
    ~TaskScheduler()
    {
        Dispose(false);
    }
}