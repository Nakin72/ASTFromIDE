// AstroEditor.Core/Alarms/AlarmDefinition.cs
// Определение аварии — шаблон (системный или пользовательский)

using System.Text.Json.Serialization;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Alarms;

/// <summary>
/// Определение аварии. Может быть системной (Core/System) или пользовательской (Vendor/User).
/// </summary>
public class AlarmDefinition
{
    /// <summary>Уникальный числовой код аварии.</summary>
    public int Code { get; set; }

    /// <summary>Категория аварии (Core, System, Vendor, User).</summary>
    public DataTypeCategory Category { get; set; } = DataTypeCategory.User;

    /// <summary>Тяжесть аварии.</summary>
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Error;

    /// <summary>Короткое имя (идентификатор).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Сообщение, отображаемое при активации. Поддерживает {0} {1} для подстановки.</summary>
    public string MessageTemplate { get; set; } = string.Empty;

    /// <summary>Описание аварии.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Причина (рекомендация оператору).</summary>
    public string? Cause { get; set; }

    /// <summary>Действие по устранению.</summary>
    public string? Remedy { get; set; }

    /// <summary>Требуется ли квитирование оператором.</summary>
    public bool RequiresAcknowledge { get; set; } = true;

    /// <summary>Автоматически сбрасывается при исчезновении условия.</summary>
    public bool AutoReset { get; set; } = false;

    /// <summary>Задержка перед активацией (мс).</summary>
    public int DebounceMs { get; set; } = 0;

    /// <summary>Останавливать ли выполнение программы при активации.</summary>
    public bool Fatal { get; set; } = false;

    [JsonIgnore]
    public string FullName => $"{Category}/{Name}";

    public string FormatMessage(params object?[] args) =>
        args.Length > 0 ? string.Format(MessageTemplate, args) : MessageTemplate;
}
