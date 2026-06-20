// AstroEditor.Core/Alarms/AlarmInstance.cs
// Экземпляр аварии — активная/историческая запись

using System.Text.Json.Serialization;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Alarms;

/// <summary>
/// Активный или исторический экземпляр аварии.
/// </summary>
public class AlarmInstance
{
    /// <summary>Ссылка на определение (по коду).</summary>
    public int Code { get; set; }

    /// <summary>Текущее состояние.</summary>
    public AlarmState State { get; set; } = AlarmState.Inactive;

    /// <summary>Время возникновения.</summary>
    public DateTime? RaisedAt { get; set; }

    /// <summary>Время квитирования.</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Время сброса.</summary>
    public DateTime? ClearedAt { get; set; }

    /// <summary>Значения подстановки в сообщение.</summary>
    public List<object?> Arguments { get; set; } = new();

    /// <summary>Строка программы, вызвавшая аварию (если из интерпретатора).</summary>
    public int? SourceLine { get; set; }

    /// <summary>Имя программы, вызвавшей аварию.</summary>
    public string? SourceProgram { get; set; }

    /// <summary>Счётчик повторных срабатываний.</summary>
    public int OccurrenceCount { get; set; } = 1;

    [JsonIgnore]
    public TimeSpan? ActiveDuration =>
        RaisedAt.HasValue && ClearedAt.HasValue
            ? ClearedAt.Value - RaisedAt.Value
            : RaisedAt.HasValue ? DateTime.UtcNow - RaisedAt.Value : null;

    public AlarmInstance Clone() => new()
    {
        Code = Code,
        State = State,
        RaisedAt = RaisedAt,
        AcknowledgedAt = AcknowledgedAt,
        ClearedAt = ClearedAt,
        Arguments = new List<object?>(Arguments),
        SourceLine = SourceLine,
        SourceProgram = SourceProgram,
        OccurrenceCount = OccurrenceCount
    };
}
