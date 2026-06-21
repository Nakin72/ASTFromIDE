// AstroEditor.Core/Data/Services/IRuntimeService.cs
// Интерфейс runtime-сервиса (аварии, прерывания, таймеры)

using AstroEditor.Core.Alarms;
using AstroEditor.Core.Execution;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Интерфейс runtime-сервиса — управление исполнением.
/// </summary>
public interface IRuntimeService
{
    /// <summary>Сервис аварий.</summary>
    IAlarmService Alarms { get; }
    
    /// <summary>Сервис прерываний.</summary>
    IInterruptService Interrupts { get; }
    
    /// <summary>Сервис таймеров.</summary>
    ITimerService Timers { get; }
    
    /// <summary>Инициализировать runtime-компоненты.</summary>
    void Initialize();
    
    /// <summary>Запустить таймеры.</summary>
    void StartTimers();
    
    /// <summary>Остановить таймеры.</summary>
    void StopTimers();
}
