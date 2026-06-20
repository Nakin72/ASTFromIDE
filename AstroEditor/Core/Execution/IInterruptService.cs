// AstroEditor/Core/Execution/IInterruptService.cs
// Интерфейс сервиса прерываний — для внедрения зависимостей

namespace AstroEditor.Core.Execution;

/// <summary>
/// Сервис управления прерываниями.
/// </summary>
public interface IInterruptService
{
    IReadOnlyDictionary<string, InterruptDefinition> Definitions { get; }
    bool HasDeferred { get; }

    void Register(InterruptDefinition definition);
    bool Unregister(string id);
    InterruptDefinition? GetDefinition(string id);
    InterruptDefinition? GetDefinitionByName(string name);
    bool Enable(string id);
    bool Disable(string id);
    void Fire(InterruptDefinition def);
    InterruptDefinition? DequeueDeferred();
    void ClearDeferred();
    void CheckValueConditions();
    void CheckAlarmConditions(int alarmCode);
}