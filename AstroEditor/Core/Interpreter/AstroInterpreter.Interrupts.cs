// AstroEditor.Core/Interpreter/AstroInterpreter.Interrupts.cs
// Обработчики прерываний: Declare, On, Off

using AstroEditor.Core.Programs;
using AstroEditor.Core.Common;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Execution;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreterEx
{
    [InstructionHandler("core.interrupt.declare")]
    private void ExecuteInterruptDeclare(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var triggerTypeField = GetFieldValue<ConstantFieldValue>(instruction, "triggerType");
        var handlerField = GetFieldValue<ConstantFieldValue>(instruction, "handlerProgram");

        var triggerType = triggerTypeField.Value?.ToString() switch
        {
            "OnChange" => InterruptTrigger.OnChange,
            "OnRisingEdge" => InterruptTrigger.OnRisingEdge,
            "OnFallingEdge" => InterruptTrigger.OnFallingEdge,
            "OnValue" => InterruptTrigger.OnValue,
            "OnTimer" => InterruptTrigger.OnTimer,
            "OnAlarm" => InterruptTrigger.OnAlarm,
            _ => InterruptTrigger.OnChange
        };

        var execModeStr = "Deferred";
        if (TryGetFieldValue<ConstantFieldValue>(instruction, "executionMode", out var execField))
            execModeStr = execField.Value?.ToString() ?? "Deferred";

        var execMode = execModeStr switch
        {
            "Deferred" => InterruptExecutionMode.Deferred,
            "Background" => InterruptExecutionMode.Background,
            "Inline" => InterruptExecutionMode.Inline,
            _ => InterruptExecutionMode.Deferred
        };

        var def = new InterruptDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = nameField.Value?.ToString() ?? "Unnamed",
            TriggerType = triggerType,
            Expression = TryGetFieldValue<ExpressionFieldValue>(instruction, "expression", out var exprF)
                ? exprF.Expression : null,
            VariableName = TryGetFieldValue<VariableFieldValue>(instruction, "variableName", out var varF)
                ? varF.VariableName : null,
            AlarmCode = TryGetFieldValue<ConstantFieldValue>(instruction, "alarmCode", out var alarmF)
                ? Convert.ToInt32(alarmF.Value) : null,
            TimerIntervalMs = TryGetFieldValue<ConstantFieldValue>(instruction, "timerMs", out var timerF)
                ? Convert.ToInt32(timerF.Value) : null,
            HandlerProgramName = handlerField.Value?.ToString() ?? string.Empty,
            ExecutionMode = execMode,
            IsEnabled = true
        };

        _context.InterruptService?.Register(def);
    }

    [InstructionHandler("core.interrupt.on")]
    private void ExecuteInterruptOn(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var def = _context.InterruptService?.GetDefinitionByName(name);
            if (def != null)
                _context.InterruptService?.Enable(def.Id);
        }
    }

    [InstructionHandler("core.interrupt.off")]
    private void ExecuteInterruptOff(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var def = _context.InterruptService?.GetDefinitionByName(name);
            if (def != null)
                _context.InterruptService?.Disable(def.Id);
        }
    }
}
