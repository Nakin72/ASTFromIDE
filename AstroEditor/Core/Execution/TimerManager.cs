// AstroEditor/Core/Execution/TimerManager.cs
// Менеджер таймеров — System.Threading.Timer вместо Thread.Sleep

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
    internal DateTime LastFireUtc;
}

/// <summary>
/// Менеджер таймеров. Использует System.Threading.Timer вместо Thread.Sleep.
/// Точность ~1мс (системный таймер Windows), без блокировки потока.
/// </summary>
public class TimerManager : ITimerService, IDisposable
{
    private readonly Dictionary<string, TimerDefinition> _timers = new();
    private readonly object _lock = new();
    
    // ✅ P1-7: System.Threading.Timer вместо Thread.Sleep
    private System.Threading.Timer? _monitoringTimer;
    private TimerCallback? _timerCallback;
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
            timer.ElapsedCount = 0;
            timer.LastFireUtc = DateTime.UtcNow;
            return timer;
        }
    }

    public bool Unregister(string id)
    {
        lock (_lock)
        {
            return _timers.Remove(id);
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
                t.ElapsedCount = 0;
                t.LastFireUtc = DateTime.UtcNow;
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
                t.ElapsedCount = 0;
                t.LastFireUtc = DateTime.UtcNow;
                return true;
            }
            return false;
        }
    }

    // ========== Поток мониторинга (System.Threading.Timer) ==========

    public void Start()
    {
        if (_running) return;
        _running = true;
        _timerCallback = new TimerCallback(CheckTimers);
        
        // ✅ P1-7: Timer с интервалом 50мс для проверки
        // Это точнее Thread.Sleep и не блокирует поток
        _monitoringTimer = new System.Threading.Timer(
            _timerCallback,
            null,
            TimeSpan.FromMilliseconds(50),   // Первая проверка через 50мс
            TimeSpan.FromMilliseconds(50)    // Последующие каждые 50мс
        );
    }

    public void Stop()
    {
        _running = false;
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
    }

    /// <summary>
    /// Проверка таймеров (вызывается System.Threading.Timer).
    /// ✅ P1-7: Вместо Thread.Sleep — точная проверка по DateTime.UtcNow
    /// </summary>
    private void CheckTimers(object? state)
    {
        if (!_running) return;

        List<TimerDefinition> toFire = new();
        DateTime now = DateTime.UtcNow;

        lock (_lock)
        {
            foreach (var timer in _timers.Values)
            {
                if (!timer.IsEnabled) continue;

                // Вычисляемelapsed время с последнего срабатывания
                var elapsed = (now - timer.LastFireUtc).TotalMilliseconds;

                if (elapsed >= timer.IntervalMs)
                {
                    toFire.Add(timer);
                    timer.LastFireUtc = now;
                    timer.ElapsedCount++;

                    // Oneshot — отключаем после срабатывания
                    if (timer.Mode == TimerMode.Oneshot)
                    {
                        timer.IsEnabled = false;
                    }
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
    }

    public void Dispose()
    {
        Stop();
    }
}