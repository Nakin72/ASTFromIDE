// AstroEditor.Core/Interpreter/AstroInterpreter.Alarms.cs
// Обработчики аварий: Raise, Clear, Acknowledge, ClearAll

using AstroEditor.Core.Programs;
using AstroEditor.Core.Common;
using AstroEditor.Core.Alarms;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    [InstructionHandler("core.alarm.raise")]
    private void ExecuteAlarmRaise(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);

        var alarmSvc = _context.AlarmService;
        if (alarmSvc == null)
            throw new Exception("AlarmService not available");

        var def = alarmSvc.GetDefinition(code);
        if (def == null)
        {
            var severityStr = "Error";
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "severity", out var sevField))
                severityStr = sevField.Value?.ToString() ?? "Error";

            var severity = severityStr switch
            {
                "Info" => AlarmSeverity.Info,
                "Warning" => AlarmSeverity.Warning,
                "Error" => AlarmSeverity.Error,
                "Fatal" => AlarmSeverity.Fatal,
                _ => AlarmSeverity.Error
            };

            def = alarmSvc.CreateUserAlarm($"ALARM_{code}", $"Alarm #{code}", severity);
            def.Fatal = severity == AlarmSeverity.Fatal;
        }

        var instance = alarmSvc.RaiseFromProgram(
            code,
            _state.Program.Name,
            _state.CurrentLineIndex < _state.Program.Lines.Count
                ? _state.Program.Lines[_state.CurrentLineIndex].LineNumber
                : null
        );

        if (def.Fatal)
        {
            _state.StopRequested = true;
            throw new AlarmFatalException(code, def.Name, def.FormatMessage());
        }
    }

    [InstructionHandler("core.alarm.clear")]
    private void ExecuteAlarmClear(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);
        _context.AlarmService?.Clear(code);
    }

    [InstructionHandler("core.alarm.ack")]
    private void ExecuteAlarmAck(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);
        _context.AlarmService?.Acknowledge(code);
    }

    [InstructionHandler("core.alarm.clearall")]
    private void ExecuteAlarmClearAll(Instruction instruction)
    {
        _context.AlarmService?.ClearAll();
    }
}
