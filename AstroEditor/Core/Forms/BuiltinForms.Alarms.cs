// AstroEditor.Core/Forms/BuiltinForms.Alarms.cs
// Формы аварий: Raise, Clear, Ack, ClearAll

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateAlarmRaiseForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.raise",
            Name = "RaiseAlarm",
            Category = "Alarm",
            Description = "Поднять аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "message",
                    DisplayName = "Сообщение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "severity",
                    DisplayName = "Тяжесть",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Info", "Warning", "Error", "Fatal" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Error")
                }
            }
        };
    }

    public static FormDefinition CreateAlarmClearForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.clear",
            Name = "ClearAlarm",
            Category = "Alarm",
            Description = "Сбросить аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateAlarmAckForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.ack",
            Name = "AckAlarm",
            Category = "Alarm",
            Description = "Квитировать аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateAlarmClearAllForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.clearall",
            Name = "ClearAllAlarms",
            Category = "Alarm",
            Description = "Сбросить все аварии",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
