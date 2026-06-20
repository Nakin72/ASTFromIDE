// AstroEditor.Core/Forms/BuiltinForms.Execution.cs
// Формы исполнения: прерывания, таймеры, WAIT

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    #region Прерывания

    public static FormDefinition CreateInterruptDeclareForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.declare",
            Name = "DeclareInterrupt",
            Category = "Interrupt",
            Description = "Объявить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "triggerType",
                    DisplayName = "Тип триггера",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "OnChange", "OnRisingEdge", "OnFallingEdge", "OnValue", "OnTimer", "OnAlarm" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "expression",
                    DisplayName = "Выражение (для OnChange/OnValue)",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "variableName",
                    DisplayName = "Переменная (для OnChange)",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "alarmCode",
                    DisplayName = "Код аварии (для OnAlarm)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "timerMs",
                    DisplayName = "Интервал таймера мс (для OnTimer)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "handlerProgram",
                    DisplayName = "Программа-обработчик",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "executionMode",
                    DisplayName = "Режим выполнения",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Deferred", "Background", "Inline" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Deferred")
                }
            }
        };
    }

    public static FormDefinition CreateInterruptOnForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.on",
            Name = "InterruptOn",
            Category = "Interrupt",
            Description = "Включить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateInterruptOffForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.off",
            Name = "InterruptOff",
            Category = "Interrupt",
            Description = "Выключить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    #endregion

    #region Таймеры

    public static FormDefinition CreateTimerDeclareForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.declare",
            Name = "DeclareTimer",
            Category = "Timer",
            Description = "Объявить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "intervalMs",
                    DisplayName = "Интервал (мс)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "mode",
                    DisplayName = "Режим",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Periodic", "Oneshot" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Periodic")
                },
                new FormFieldDefinition
                {
                    Name = "handlerProgram",
                    DisplayName = "Программа-обработчик",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false
                }
            }
        };
    }

    public static FormDefinition CreateTimerOnForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.on",
            Name = "TimerOn",
            Category = "Timer",
            Description = "Запустить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateTimerOffForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.off",
            Name = "TimerOff",
            Category = "Timer",
            Description = "Остановить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateTimerResetForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.reset",
            Name = "TimerReset",
            Category = "Timer",
            Description = "Сбросить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    #endregion

    #region WAIT

    public static FormDefinition CreateWaitForm()
    {
        return new FormDefinition
        {
            Id = "core.wait",
            Name = "Wait",
            Category = "Control",
            Description = "Ожидание по времени или по условию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "mode",
                    DisplayName = "Режим ожидания",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Time", "Condition" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "timeMs",
                    DisplayName = "Время ожидания (мс)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(1000)
                },
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "timeoutMs",
                    DisplayName = "Таймаут (мс, 0 = бесконечно)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(0)
                }
            }
        };
    }

    #endregion
}
