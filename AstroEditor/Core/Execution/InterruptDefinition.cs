// AstroEditor/Core/Execution/InterruptDefinition.cs
// Определение прерывания

using AstroEditor.Core.Common;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Execution;

/// <summary>
/// Описание прерывания/события.
/// </summary>
public class InterruptDefinition
{
    /// <summary>Уникальный ID прерывания.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Имя прерывания (для отображения).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Тип триггера.</summary>
    public InterruptTrigger TriggerType { get; set; } = InterruptTrigger.OnChange;

    /// <summary>Выражение-условие срабатывания (для OnValue/OnChange).</summary>
    public string? Expression { get; set; }

    /// <summary>Имя переменной для отслеживания (для OnChange/OnValue).</summary>
    public string? VariableName { get; set; }

    /// <summary>Код аварии для отслеживания (для OnAlarm).</summary>
    public int? AlarmCode { get; set; }

    /// <summary>Интервал таймера в мс (для OnTimer).</summary>
    public int? TimerIntervalMs { get; set; }

    /// <summary>Имя программы-обработчика.</summary>
    public string HandlerProgramName { get; set; } = string.Empty;

    /// <summary>Режим выполнения обработчика.</summary>
    public InterruptExecutionMode ExecutionMode { get; set; } = InterruptExecutionMode.Deferred;

    /// <summary>Активно ли прерывание.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Приоритет (меньше число = выше приоритет).</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Описание.</summary>
    public string? Description { get; set; }
}
