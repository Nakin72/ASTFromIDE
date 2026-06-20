// AstroEditor.Core/Forms/BuiltinForms.Loops.cs
// Формы циклов: WHILE, FOR, FOREACH

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
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

    public static FormDefinition CreateEndWhileForm()
    {
        return new FormDefinition
        {
            Id = "core.endwhile",
            Name = "EndWhile",
            Category = "Logic",
            Description = "Конец цикла WHILE",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
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

    public static FormDefinition CreateForEachForm()
    {
        return new FormDefinition
        {
            Id = "core.foreach",
            Name = "ForEach",
            Category = "Logic",
            Description = "Цикл по элементам коллекции",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "FOREACH",
                ClosingKeyword = "ENDFOREACH",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "itemVariable",
                    DisplayName = "Переменная элемента",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "collection",
                    DisplayName = "Коллекция (массив)",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateEndForEachForm()
    {
        return new FormDefinition
        {
            Id = "core.endforeach",
            Name = "EndForEach",
            Category = "Logic",
            Description = "Конец цикла FOR EACH",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
