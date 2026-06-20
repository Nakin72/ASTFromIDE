// AstroEditor.Core/Interpreter/InstructionHandlerAttribute.cs
// Атрибут для маркировки методов-обработчиков инструкций

namespace AstroEditor.Core.Interpreter;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InstructionHandlerAttribute : Attribute
{
    public string FormId { get; }

    public InstructionHandlerAttribute(string formId)
    {
        FormId = formId;
    }
}
