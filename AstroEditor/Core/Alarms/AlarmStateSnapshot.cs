// AstroEditor.Core/Alarms/AlarmStateSnapshot.cs
// Снэпшот состояния аварий для сериализации

namespace AstroEditor.Core.Alarms;

/// <summary>
/// Сохраняемый срез состояния аварий.
/// </summary>
public class AlarmStateSnapshot
{
    public List<AlarmDefinition> Definitions { get; set; } = new();
    public List<AlarmInstance> ActiveAlarms { get; set; } = new();
    public List<AlarmInstance> History { get; set; } = new();
}
