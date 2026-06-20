// AstroEditor.Core/Forms/BuiltinForms.Exceptions.cs
// Формы для обработки исключений: TRY, CATCH, FINALLY, ENDTRY, THROW

using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateTryForm()
    {
        return new FormDefinition
        {
            Id = "core.try",
            Name = "Try",
            Category = "Exception Handling",
            Description = "Начало блока обработки исключений",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "TRY",
                ClosingKeyword = "ENDTRY",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateCatchForm()
    {
        return new FormDefinition
        {
            Id = "core.catch",
            Name = "Catch",
            Category = "Exception Handling",
            Description = "Блок перехвата исключения",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "exceptionVariable",
                    DisplayName = "Переменная для исключения",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "string", "int", "any" },
                    Required = false,
                    Description = "Переменная, в которую будет записано сообщение об ошибке"
                },
                new FormFieldDefinition
                {
                    Name = "errorCode",
                    DisplayName = "Код ошибки (опционально)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    Description = "Перехватывать только эту ошибку (0 = любая)"
                }
            }
        };
    }

    public static FormDefinition CreateFinallyForm()
    {
        return new FormDefinition
        {
            Id = "core.finally",
            Name = "Finally",
            Category = "Exception Handling",
            Description = "Блок, выполняемый всегда",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndTryForm()
    {
        return new FormDefinition
        {
            Id = "core.endtry",
            Name = "EndTry",
            Category = "Exception Handling",
            Description = "Конец блока обработки исключений",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateThrowForm()
    {
        return new FormDefinition
        {
            Id = "core.throw",
            Name = "Throw",
            Category = "Exception Handling",
            Description = "Выбросить исключение",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "errorCode",
                    DisplayName = "Код ошибки",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true,
                    Description = "Код ошибки (пользовательский или системный)"
                },
                new FormFieldDefinition
                {
                    Name = "message",
                    DisplayName = "Сообщение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    Description = "Текст ошибки"
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

    public static FormDefinition CreateRethrowForm()
    {
        return new FormDefinition
        {
            Id = "core.rethrow",
            Name = "Rethrow",
            Category = "Exception Handling",
            Description = "Повторно выбросить текущее исключение",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }
}
