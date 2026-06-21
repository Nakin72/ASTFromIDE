// AstroEditor.Core/Data/Services/RuntimeService.cs
// Runtime-сервис (аварии, прерывания, таймеры)

using AstroEditor.Core.Alarms;
using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Execution;
using Microsoft.Extensions.Logging;
using AstroScheduler = AstroEditor.Core.Execution.TaskScheduler;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Runtime-сервис — управление исполнением.
/// </summary>
public class RuntimeService : IRuntimeService
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;
    private AlarmManager? _alarmManager;
    private InterruptManager? _interruptManager;
    private TimerManager? _timerManager;
    private AstroScheduler? _scheduler;

    public IAlarmService Alarms
    {
        get
        {
            _alarmManager ??= CreateAlarmManager();
            return _alarmManager;
        }
    }

    public IInterruptService Interrupts
    {
        get
        {
            if (_interruptManager == null)
            {
                _interruptManager = new InterruptManager
                {
                    GlobalTables = _state.GlobalTables,
                    ProgramRegistry = _state.Programs,
                    TypeRegistry = _state.TypeRegistry,
                    AlarmManager = _alarmManager,
                    Scheduler = _scheduler
                };
            }
            return _interruptManager;
        }
    }

    public ITimerService Timers
    {
        get
        {
            if (_timerManager == null)
            {
                _timerManager = new TimerManager
                {
                    Interrupts = _interruptManager,
                    Scheduler = _scheduler,
                    ProgramRegistry = _state.Programs
                };
                _timerManager.Start();
            }
            return _timerManager;
        }
    }

    public RuntimeService(ProjectState state, AstroScheduler? scheduler = null, ILogger? logger = null)
    {
        _state = state;
        _scheduler = scheduler;
        _logger = logger ?? Log.For<RuntimeService>();
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing runtime services");
        
        // Инициализация аварий
        _alarmManager = CreateAlarmManager();
        _logger.LogDebug("Alarm manager initialized");
    }

    public void StartTimers()
    {
        if (_timerManager != null)
        {
            _timerManager.Start();
            _logger.LogInformation("Timers started");
        }
    }

    public void StopTimers()
    {
        if (_timerManager != null)
        {
            _timerManager.Stop();
            _logger.LogInformation("Timers stopped");
        }
    }

    private AlarmManager CreateAlarmManager()
    {
        var mgr = new AlarmManager();
        mgr.RegisterSystemAlarms();
        return mgr;
    }
}
