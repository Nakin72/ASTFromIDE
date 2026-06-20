// AstroEditor/Core/Execution/TimerManager.cs
// Менеджер таймеров — точные интервалы, периодические и однократные срабатывания

using System.Diagnostics;
using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Execution;

/// <summary>
/// Описание таймера.
/// </summary>
public class TimerDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    /// <summary>Интервал в миллисекундах.</summary>
    public int IntervalMs { get; set; } = 1000;
    /// <summary>Тип — однократный или периодический.</summary>
    public TimerMode Mode { get; set; } = TimerMode.Periodic;
    /// <summary>Имя программы-обработчика (опционально).</summary>
    public string? HandlerProgramName { get; set; }
    /// <summary>Имя прерывания для триггера (опционально).</summary>
    public string? InterruptId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }

    // Внутренние счётчики
    internal int ElapsedCount;
    internal Stopwatch Stopwatch = new();
}

/// <summary>
/// Менеджер таймеров. Запускает поток с точным ожиданием.
/// </summary>
public class TimerManager : ITimerService, IDisposable
{
    private readonly Dictionary<string, TimerDefinition> _timers = new();
    private readonly object _lock = new();
    private Thread? _timerThread;
    private volatile bool _running;

    /// <summary>Ссылка на прерывания, чтобы триггерить таймерные прерывания.</summary>
    public InterruptManager? Interrupts { get; set; }

    /// <summary>Ссылка на планировщик для запуска обработчиков.</summary>
    public ITaskScheduler? Scheduler { get; set; }

    /// <summary>Реестр программ (для запуска обработчиков).</summary>
    public Dictionary<string, AstroProgram>? ProgramRegistry { get; set; }

    /// <summary>Событие: таймер сработал.</summary>
    public event Action<TimerDefinition>? OnTimerElapsed;
    /// <summary>Событие: таймер запущен.</summary>
    public event Action<TimerDefinition>? OnTimerStarted;
    /// <summary>Событие: таймер остановлен.</summary>
    public event Action<TimerDefinition>? OnTimerStopped;
    /// <summary>Событие: ошибка при запуске обработчика таймера.</summary>
    public event Action<TimerDefinition, Exception>? OnTimerError;

    public IReadOnlyDictionary<string, TimerDefinition> Timers => _timers;

    // ========== Управление таймерами ==========

    public TimerDefinition Register(TimerDefinition timer)
    {
        lock (_lock)
        {
            _timers[timer.Id] = timer;
            timer.Stopwatch.Reset();
            timer.ElapsedCount = 0;
            if (timer.IsEnabled)
                timer.Stopwatch.Start();
            return timer;
        }
    }

    public bool Unregister(string id)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Stopwatch.Stop();
                return _timers.Remove(id);
            }
            return false;
        }
    }

    public TimerDefinition? GetTimer(string id)
    {
        lock (_lock) { return _timers.GetValueOrDefault(id); }
    }

    public bool Enable(string id)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                t.IsEnabled = true;
                t.Stopwatch.Restart();
                t.ElapsedCount = 0;
                return true;
            }
            return false;
        }
    }

    public bool Disable(string id)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                t.IsEnabled = false;
                t.Stopwatch.Reset();
                return true;
            }
            return false;
        }
    }

    public bool Reset(string id)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                t.Stopwatch.Restart();
                t.ElapsedCount = 0;
                return true;
            }
            return false;
        }
    }

    // ========== Поток мониторинга ==========

    public void Start()
    {
        if (_running) return;
        _running = true;
        _timerThread = new Thread(TimerLoop)
        {
            Name = "AST-TimerManager",
            IsBackground = true
        };
        _timerThread.Start();
    }

    public void Stop()
    {
        _running = false;
        _timerThread?.Join(1000);
    }

    private void TimerLoop()
    {
        while (_running)
        {
            List<TimerDefinition> toFire = new();
            int minWaitMs = 1000;

            lock (_lock)
            {
                var now = DateTime.UtcNow;

                foreach (var timer in _timers.Values)
                {
                    if (!timer.IsEnabled) continue;

                    var elapsed = timer.Stopwatch.ElapsedMilliseconds;
                    var remaining = timer.IntervalMs - elapsed;

                    if (remaining <= 0)
                    {
                        // Сработал
                        toFire.Add(timer);

                        if (timer.Mode == TimerMode.Periodic)
                        {
                            // Перезапускаем, учитывая переполнение
                            timer.Stopwatch.Restart();
                            timer.ElapsedCount++;
                        }
                        else
                        {
                            // Oneshot — отключаем
                            timer.IsEnabled = false;
                            timer.Stopwatch.Reset();
                        }
                    }
                    else
                    {
                        if (remaining < minWaitMs)
                            minWaitMs = (int)remaining;
                    }
                }
            }

            // Обрабатываем сработавшие (вне блокировки)
            foreach (var timer in toFire)
            {
                try
                {
                    OnTimerElapsed?.Invoke(timer);

                    // Если есть обработчик — запускаем
                    if (!string.IsNullOrEmpty(timer.HandlerProgramName) &&
                        ProgramRegistry != null &&
                        ProgramRegistry.TryGetValue(timer.HandlerProgramName, out var handlerProg) &&
                        Scheduler != null)
                    {
                        var config = new TaskConfig
                        {
                            Name = $"TIMER-{timer.Name}",
                            Program = handlerProg,
                            Type = TaskType.Background,
                            Priority = TaskPriority.Normal,
                            MaxCycles = 1
                        };
                        Scheduler.StartTask(config);
                    }

                    // Если есть прерывание по таймеру — триггерим
                    if (!string.IsNullOrEmpty(timer.InterruptId) && Interrupts != null)
                    {
                        var def = Interrupts.GetDefinition(timer.InterruptId);
                        if (def != null)
                            Interrupts.Fire(def);
                    }
                }
                catch (Exception ex)
                {
                    OnTimerError?.Invoke(timer, ex);
                }
            }

            // Ожидаем до следующей проверки
            if (_running)
            {
                var sleepMs = Math.Max(1, Math.Min(minWaitMs, 50));
                Thread.Sleep(sleepMs);
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}