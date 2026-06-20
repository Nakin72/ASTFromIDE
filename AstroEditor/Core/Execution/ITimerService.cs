// AstroEditor/Core/Execution/ITimerService.cs
// Интерфейс сервиса таймеров — для внедрения зависимостей

namespace AstroEditor.Core.Execution;

/// <summary>
/// Сервис управления таймерами.
/// </summary>
public interface ITimerService
{
    IReadOnlyDictionary<string, TimerDefinition> Timers { get; }

    TimerDefinition Register(TimerDefinition definition);
    bool Unregister(string name);
    TimerDefinition? GetTimer(string name);
    bool Enable(string name);
    bool Disable(string name);
    bool Reset(string name);
    void Start();
    void Stop();

    event Action<TimerDefinition>? OnTimerElapsed;
    event Action<TimerDefinition>? OnTimerStarted;
    event Action<TimerDefinition>? OnTimerStopped;
}