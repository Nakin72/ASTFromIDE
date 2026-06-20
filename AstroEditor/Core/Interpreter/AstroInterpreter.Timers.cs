// AstroEditor.Core/Interpreter/AstroInterpreter.Timers.cs
// Обработчики таймеров: Declare, On, Off, Reset

using AstroEditor.Core.Programs;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    [InstructionHandler("core.timer.declare")]
    private void ExecuteTimerDeclare(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var intervalField = GetFieldValue<ConstantFieldValue>(instruction, "intervalMs");

        var modeStr = "Periodic";
        if (TryGetFieldValue<ConstantFieldValue>(instruction, "mode", out var modeField))
            modeStr = modeField.Value?.ToString() ?? "Periodic";

        var mode = modeStr == "Oneshot" ? TimerMode.Oneshot : TimerMode.Periodic;

        var timerDef = new TimerDefinition
        {
            Name = nameField.Value?.ToString() ?? "UnnamedTimer",
            IntervalMs = Convert.ToInt32(intervalField.Value),
            Mode = mode,
            HandlerProgramName = TryGetFieldValue<ConstantFieldValue>(instruction, "handlerProgram", out var hf)
                ? hf.Value?.ToString() : null
        };

        _context.TimerService?.Register(timerDef);
    }

    [InstructionHandler("core.timer.on")]
    private void ExecuteTimerOn(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Enable(timer.Id);
        }
    }

    [InstructionHandler("core.timer.off")]
    private void ExecuteTimerOff(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Disable(timer.Id);
        }
    }

    [InstructionHandler("core.timer.reset")]
    private void ExecuteTimerReset(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Reset(timer.Id);
        }
    }
}
