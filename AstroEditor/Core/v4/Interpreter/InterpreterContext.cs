// AstroEditor.Core.v4/Interpreter/InterpreterContext.cs
using AstroEditor.Core.v4.Types;
using AstroEditor.Core.v4.Tables;
using AstroEditor.Core.v4.Forms;
using AstroEditor.Core.v4.Expressions;
using AstroEditor.Core.v4.Programs;
// AstroEditor.Core.v4/Interpreter/InterpreterContext.cs
namespace AstroEditor.Core.v4.Interpreter;

public class InterpreterContext
{
    public DataTypeRegistry TypeRegistry { get; set; } = null!;
    public FormRegistry FormRegistry { get; set; } = null!;
    public VariableTableSet GlobalTables { get; set; } = null!;
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; } = new();
    public Dictionary<string, AstroProgram> ProgramRegistry { get; set; } = new();

    // События
    public event Action<InterpreterState, Instruction>? OnBeforeInstruction;
    public event Action<InterpreterState, Instruction>? OnAfterInstruction;
    public event Action<InterpreterState, Exception>? OnError;

    // Методы для вызова событий
    public void RaiseOnBeforeInstruction(InterpreterState state, Instruction instruction)
        => OnBeforeInstruction?.Invoke(state, instruction);

    public void RaiseOnAfterInstruction(InterpreterState state, Instruction instruction)
        => OnAfterInstruction?.Invoke(state, instruction);

    public void RaiseOnError(InterpreterState state, Exception ex)
        => OnError?.Invoke(state, ex);
}