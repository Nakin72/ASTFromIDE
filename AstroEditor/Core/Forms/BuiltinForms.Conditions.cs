// AstroEditor.Core/Forms/BuiltinForms.Conditions.cs
// Формы условий: IF, ELSE, SWITCH, CASE, DEFAULT

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateIfForm()
    {
        return new FormDefinition
        {
            Id = "core.if",
            Name = "If",
            Category = "Logic",
            Description = "Условный оператор",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "IF",
                ClosingKeyword = "ENDIF",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateElseForm()
    {
        return new FormDefinition
        {
            Id = "core.else",
            Name = "Else",
            Category = "Logic",
            Description = "Иначе",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndIfForm()
    {
        return new FormDefinition
        {
            Id = "core.endif",
            Name = "EndIf",
            Category = "Logic",
            Description = "Конец условия",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateSwitchForm()
    {
        return new FormDefinition
        {
            Id = "core.switch",
            Name = "Switch",
            Category = "Logic",
            Description = "Множественный выбор",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "SWITCH",
                ClosingKeyword = "ENDSWITCH",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "expression",
                    DisplayName = "Выражение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateCaseForm()
    {
        return new FormDefinition
        {
            Id = "core.case",
            Name = "Case",
            Category = "Logic",
            Description = "Ветка выбора",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "value",
                    DisplayName = "Значение",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateDefaultForm()
    {
        return new FormDefinition
        {
            Id = "core.default",
            Name = "Default",
            Category = "Logic",
            Description = "Ветка по умолчанию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndSwitchForm()
    {
        return new FormDefinition
        {
            Id = "core.endswitch",
            Name = "EndSwitch",
            Category = "Logic",
            Description = "Конец Switch",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
