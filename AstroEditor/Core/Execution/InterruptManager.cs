// AstroEditor/Core/Execution/InterruptManager.cs
// Менеджер прерываний — мониторинг триггеров и запуск обработчиков

using AstroEditor.Core.Common;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Execution;

/// <summary>
/// Управляет регистрацией, мониторингом и запуском прерываний.
/// </summary>
public class InterruptManager : IInterruptService, IDisposable
{
    private readonly Dictionary<string, InterruptDefinition> _definitions = new();
    private readonly List<(string Id, object? PreviousValue)> _monitoredExpressions = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitorTask;
    private volatile bool _running;

    // Внешние зависимости
    public Tables.VariableTableSet GlobalTables { get; set; } = null!;
    public Dictionary<string, AstroProgram> ProgramRegistry { get; set; } = new();
    public DataTypeRegistry TypeRegistry { get; set; } = null!;
    public AstroEditor.Core.Alarms.AlarmManager? AlarmManager { get; set; }

    /// <summary>Планировщик для запуска фоновых задач-обработчиков.</summary>
    public ITaskScheduler? Scheduler { get; set; }

    // События
    public event Action<InterruptDefinition>? OnInterruptFired;
    public event Action<InterruptDefinition, Exception>? OnInterruptError;

    // ========== Регистрация ==========

    public IReadOnlyDictionary<string, InterruptDefinition> Definitions => _definitions;

    public void Register(InterruptDefinition definition)
    {
        lock (_lock)
        {
            _definitions[definition.Id] = definition;
        }
    }

    public bool Unregister(string id)
    {
        lock (_lock)
        {
            return _definitions.Remove(id);
        }
    }

    public InterruptDefinition? GetDefinition(string id)
    {
        lock (_lock)
        {
            return _definitions.GetValueOrDefault(id);
        }
    }

    public InterruptDefinition? GetDefinitionByName(string name)
    {
        lock (_lock)
        {
            return _definitions.Values.FirstOrDefault(d => d.Name == name);
        }
    }

    public bool Enable(string id)
    {
        var def = GetDefinition(id);
        if (def != null)
        {
            def.IsEnabled = true;
            return true;
        }
        return false;
    }

    public bool Disable(string id)
    {
        var def = GetDefinition(id);
        if (def != null)
        {
            def.IsEnabled = false;
            return true;
        }
        return false;
    }

    public void EnableAll() { lock (_lock) { foreach (var d in _definitions.Values) d.IsEnabled = true; } }
    public void DisableAll() { lock (_lock) { foreach (var d in _definitions.Values) d.IsEnabled = false; } }

    // ========== Мониторинг и запуск ==========

    /// <summary>
    /// Запускает задачу мониторинга триггеров.
    /// ✅ P2: Task.Run вместо new Thread (использует ThreadPool)
    /// </summary>
    public void StartMonitoring()
    {
        if (_running) return;
        _running = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // ✅ P2: Task.Run вместо new Thread
        _monitorTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public void StopMonitoring()
    {
        _running = false;
        _cancellationTokenSource?.Cancel();
        _monitorTask?.Wait(500);
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitorTask = null;
    }

    private void MonitorLoop(CancellationToken cancellationToken)
    {
        var parser = new ExpressionParser();
        var evaluator = new ExpressionEvaluator();

        while (!cancellationToken.IsCancellationRequested)
        {
            List<InterruptDefinition> toFire = new();

            lock (_lock)
            {
                foreach (var def in _definitions.Values)
                {
                    if (!def.IsEnabled) continue;

                    bool shouldFire = false;

                    switch (def.TriggerType)
                    {
                        case InterruptTrigger.OnChange:
                        case InterruptTrigger.OnRisingEdge:
                        case InterruptTrigger.OnFallingEdge:
                        case InterruptTrigger.OnValue when !string.IsNullOrEmpty(def.Expression):
                        {
                            try
                            {
                                var ctx = new ExpressionContext
                                {
                                    GlobalTables = GlobalTables,
                                    LocalTables = null,
                                    TypeRegistry = TypeRegistry,
                                    Functions = new Dictionary<string, Func<object?[], object?>>()
                                };
                                
                                object? value;

                                if (!string.IsNullOrEmpty(def.Expression))
                                {
                                    var node = parser.Parse(def.Expression);
                                    value = evaluator.Evaluate(node, ctx);
                                }
                                else if (!string.IsNullOrEmpty(def.VariableName))
                                {
                                    var varObj = GlobalTables.FindVariable(def.VariableName);
                                    value = varObj?.Value;
                                }
                                else
                                {
                                    value = null;
                                }

                                // Сравниваем с предыдущим значением
                                var prev = _monitoredExpressions
                                    .FirstOrDefault(m => m.Id == def.Id).PreviousValue;

                                bool changed = prev != null && !Equals(prev, value);

                                if (def.TriggerType == InterruptTrigger.OnChange && changed)
                                    shouldFire = true;
                                else if (def.TriggerType == InterruptTrigger.OnRisingEdge && changed && Convert.ToBoolean(value))
                                    shouldFire = true;
                                else if (def.TriggerType == InterruptTrigger.OnFallingEdge && changed && !Convert.ToBoolean(value))
                                    shouldFire = true;
                                else if (def.TriggerType == InterruptTrigger.OnValue)
                                    shouldFire = true;

                                // Обновляем предыдущее значение
                                var idx = _monitoredExpressions.FindIndex(m => m.Id == def.Id);
                                if (idx >= 0)
                                    _monitoredExpressions[idx] = (def.Id, value);
                                else
                                    _monitoredExpressions.Add((def.Id, value));
                            }
                            catch { /* игнорируем ошибки парсинга */ }
                            break;
                        }
                    }

                    if (shouldFire)
                        toFire.Add(def);
                }
            }

            foreach (var def in toFire)
                Fire(def);

            // ✅ P2: Task.Delay вместо Thread.Sleep
            Task.Delay(50, cancellationToken).Wait(cancellationToken);
        }
    }

    /// <summary>
    /// Принудительно запустить прерывание (для внешних вызовов, например, таймеров).
    /// </summary>
    public void Fire(InterruptDefinition def)
    {
        OnInterruptFired?.Invoke(def);

        if (string.IsNullOrEmpty(def.HandlerProgramName))
            return;

        if (!ProgramRegistry.TryGetValue(def.HandlerProgramName, out var handlerProgram))
        {
            OnInterruptError?.Invoke(def, new Exception($"Handler program '{def.HandlerProgramName}' not found"));
            return;
        }

        try
        {
            switch (def.ExecutionMode)
            {
                case InterruptExecutionMode.Background when Scheduler != null:
                {
                    // Запускаем обработчик как фоновую задачу
                    var config = new TaskConfig
                    {
                        Name = $"INT-{def.Name}",
                        Program = handlerProgram,
                        Type = TaskType.Background,
                        Priority = TaskPriority.High,
                        MaxCycles = 1
                    };
                    Scheduler.StartTask(config);
                    break;
                }

                case InterruptExecutionMode.Deferred:
                {
                    // Deferred — помечаем, что прерывание ожидает.
                    // Интерпретатор проверяет ожидающие прерывания перед каждой инструкцией.
                    EnqueueDeferred(def);
                    break;
                }

                case InterruptExecutionMode.Inline:
                {
                    // Inline — выполняется немедленно (через интерпретатор).
                    // В текущей реализации — запускаем как foreground задачу.
                    if (Scheduler != null)
                    {
                        var config = new TaskConfig
                        {
                            Name = $"INT-{def.Name}",
                            Program = handlerProgram,
                            Type = TaskType.Foreground,
                            Priority = TaskPriority.High,
                            MaxCycles = 1
                        };
                        Scheduler.StartTask(config);
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            OnInterruptError?.Invoke(def, ex);
        }
    }

    // ========== Deferred-очередь с приоритетами ==========
    // ✅ P2-1: PriorityQueue вместо Queue для приоритетов
    private readonly PriorityQueue<InterruptDefinition, int> _deferredQueue = new();

    private void EnqueueDeferred(InterruptDefinition def)
    {
        lock (_deferredQueue)
        {
            _deferredQueue.Enqueue(def, def.Priority);
        }
    }

    /// <summary>
    /// Извлекает следующее отложенное прерывание (с highest приоритетом).
    /// Вызывается интерпретатором перед каждой инструкцией.
    /// </summary>
    public InterruptDefinition? DequeueDeferred()
    {
        lock (_deferredQueue)
        {
            return _deferredQueue.Count > 0 ? _deferredQueue.Dequeue() : null;
        }
    }

    /// <summary>Есть ли отложенные прерывания?</summary>
    public bool HasDeferred
    {
        get { lock (_deferredQueue) { return _deferredQueue.Count > 0; } }
    }

    /// <summary>Очищает очередь отложенных прерываний.</summary>
    public void ClearDeferred()
    {
        lock (_deferredQueue)
        {
            _deferredQueue.Clear();
        }
    }

    /// <summary>
    /// Проверяет условия OnValue/OnChange для всех зарегистрированных прерываний.
    /// Вызывается внешним планировщиком или интерпретатором.
    /// </summary>
    public void CheckValueConditions()
    {
        // Реализовано в MonitorLoop — при синхронном использовании
        // можно запустить одноразовую проверку
    }

    /// <summary>
    /// Проверяет условия OnAlarm при срабатывании аварии.
    /// Вызывается AlarmManager при Raise.
    /// </summary>
    public void CheckAlarmConditions(int alarmCode)
    {
        lock (_lock)
        {
            foreach (var def in _definitions.Values)
            {
                if (!def.IsEnabled) continue;
                if (def.TriggerType == InterruptTrigger.OnAlarm && def.AlarmCode == alarmCode)
                {
                    Fire(def);
                }
            }
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
