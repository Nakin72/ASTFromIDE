using AstroEditor.Core.v4.Common;
using AstroEditor.Core.v4.Programs;

namespace AstroEditor.Core.v4.Forms;

public static class BuiltinForms
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

    public static FormDefinition CreateWhileForm()
    {
        return new FormDefinition
        {
            Id = "core.while",
            Name = "While",
            Category = "Logic",
            Description = "Цикл с условием",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "WHILE",
                ClosingKeyword = "ENDWHILE",
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
                },
                new FormFieldDefinition
                {
                    Name = "maxIterations",
                    DisplayName = "Макс. итераций",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(1000)
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
    public static FormDefinition CreateForForm()
    {
        return new FormDefinition
        {
            Id = "core.for",
            Name = "For",
            Category = "Logic",
            Description = "Цикл с параметром",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "FOR",
                ClosingKeyword = "ENDFOR",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
        {
            new FormFieldDefinition
            {
                Name = "variable",
                DisplayName = "Переменная-счётчик",
                ValueType = FieldValueType.Variable,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "start",
                DisplayName = "Начальное значение",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "end",
                DisplayName = "Конечное значение",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "step",
                DisplayName = "Шаг",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = false,
                DefaultValue = new ConstantFieldValue(1)
            }
        }
        };
    }

    public static FormDefinition CreateEndForForm()
    {
        return new FormDefinition
        {
            Id = "core.endfor",
            Name = "EndFor",
            Category = "Logic",
            Description = "Конец цикла FOR",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
