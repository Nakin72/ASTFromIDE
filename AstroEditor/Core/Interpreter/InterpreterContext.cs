// AstroEditor.Core.v4/Interpreter/InterpreterContext.cs
using AstroEditor.Core.Types;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Execution;
// AstroEditor.Core.v4/Interpreter/InterpreterContext.cs
namespace AstroEditor.Core.Interpreter;

public class InterpreterContext
{
    public DataTypeRegistry TypeRegistry { get; set; } = null!;
    public FormRegistry FormRegistry { get; set; } = null!;
    public VariableTableSet GlobalTables { get; set; } = null!;
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; } = new();
    public Dictionary<string, AstroProgram> ProgramRegistry { get; set; } = new();

    // Сервисы (внедряются через конструктор или установщик)
    public IAlarmService? AlarmService { get; set; }
    public IInterruptService? InterruptService { get; set; }
    public ITimerService? TimerService { get; set; }

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