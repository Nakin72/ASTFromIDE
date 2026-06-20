// AstroEditor.Core/Forms/BuiltinForms.Core.cs
// Базовые формы: присваивание, вызов, управление программой

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateAssignmentForm()
    {
        return new FormDefinition
        {
            Id = "core.assign",
            Name = "Assign",
            Category = "Assignment",
            Description = "Присваивает значение переменной",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "variable",
                    DisplayName = "Переменная",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "int", "double", "real", "bool", "string", "position" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "expression",
                    DisplayName = "Выражение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "int", "double", "real", "bool", "string", "position" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateCallForm()
    {
        return new FormDefinition
        {
            Id = "core.call",
            Name = "Call",
            Category = "Program Control",
            Description = "Вызов программы",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "programName",
                    DisplayName = "Имя программы",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "arguments",
                    DisplayName = "Аргументы",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "resultVariable",
                    DisplayName = "Переменная результата",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                }
            }
        };
    }

    public static FormDefinition CreateLabelForm()
    {
        return new FormDefinition
        {
            Id = "core.lbl",
            Name = "Lbl",
            Category = "Program Control",
            Description = "Определяет метку",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Имя метки",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateJumpLblForm()
    {
        return new FormDefinition
        {
            Id = "core.jumplbl",
            Name = "JumpLbl",
            Category = "Program Control",
            Description = "Безусловный переход к метке",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Метка",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateJumpIfForm()
    {
        return new FormDefinition
        {
            Id = "core.jumpif",
            Name = "JumpIf",
            Category = "Program Control",
            Description = "Условный переход к метке",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Метка",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateReturnForm()
    {
        return new FormDefinition
        {
            Id = "core.return",
            Name = "Return",
            Category = "Program Control",
            Description = "Возврат из программы",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "value",
                    DisplayName = "Возвращаемое значение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                }
            }
        };
    }

    public static FormDefinition CreateBreakForm()
    {
        return new FormDefinition
        {
            Id = "core.break",
            Name = "Break",
            Category = "Program Control",
            Description = "Выход из цикла",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateContinueForm()
    {
        return new FormDefinition
        {
            Id = "core.continue",
            Name = "Continue",
            Category = "Program Control",
            Description = "Переход к следующей итерации цикла",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
