// AstroEditor/Core/Execution/IAlarmService.cs
// Интерфейс сервиса аварий — для внедрения зависимостей

using AstroEditor.Core.Alarms;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Execution;

/// <summary>
/// Сервис управления авариями. 
/// Позволяет регистрировать, поднимать, квитировать и сбрасывать аварии.
/// </summary>
public interface IAlarmService
{
    IReadOnlyDictionary<int, AlarmDefinition> Definitions { get; }
    IReadOnlyDictionary<int, AlarmInstance> ActiveAlarms { get; }
    IReadOnlyList<AlarmInstance> AlarmHistory { get; }

    void RegisterAlarm(AlarmDefinition definition);
    AlarmDefinition? GetDefinition(int code);
    AlarmDefinition CreateUserAlarm(string name, string message, AlarmSeverity severity = AlarmSeverity.Error);

    AlarmInstance Raise(int code, params object?[] args);
    AlarmInstance RaiseFromProgram(int code, string? programName, int? lineNumber, params object?[] args);
    bool Acknowledge(int code);
    int AcknowledgeAll();
    bool Clear(int code);
    int ClearAll();

    bool HasActiveAlarms { get; }
    int CountActive(AlarmSeverity? severity = null);
    bool IsFatal(int code);

    event Action<AlarmInstance>? OnAlarmRaised;
    event Action<AlarmInstance>? OnAlarmAcknowledged;
    event Action<AlarmInstance>? OnAlarmCleared;
    event Action? OnAllAlarmsCleared;
}