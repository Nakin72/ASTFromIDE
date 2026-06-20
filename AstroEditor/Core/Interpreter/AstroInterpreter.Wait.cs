// AstroEditor.Core/Interpreter/AstroInterpreter.Wait.cs
// Обработчик инструкции WAIT

using AstroEditor.Core.Programs;
using System.Diagnostics;
using AstroEditor.Core.Expressions;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    [InstructionHandler("core.wait")]
    private void ExecuteWait(Instruction instruction)
    {
        var modeField = GetFieldValue<ConstantFieldValue>(instruction, "mode");
        var mode = modeField.Value?.ToString() ?? "Time";

        if (mode == "Time")
        {
            var timeMs = 1000;
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "timeMs", out var timeField))
                timeMs = Convert.ToInt32(timeField.Value);

            Thread.Sleep(timeMs);
        }
        else if (mode == "Condition")
        {
            var timeoutMs = 0;
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "timeoutMs", out var toutField))
                timeoutMs = Convert.ToInt32(toutField.Value);

            var conditionExpr = TryGetFieldValue<ExpressionFieldValue>(instruction, "condition", out var condField)
                ? condField.Expression : null;

            if (string.IsNullOrEmpty(conditionExpr))
                return;

            var stopwatch = Stopwatch.StartNew();
            var parser = new ExpressionParser();
            var evaluator = new ExpressionEvaluator();

            while (true)
            {
                if (_state.StopRequested)
                    return;

                try
                {
                    var ctx = CreateExpressionContext();
                    var node = parser.Parse(conditionExpr);
                    var result = evaluator.Evaluate(node, ctx);

                    if (Convert.ToBoolean(result))
                        return;
                }
                catch { /* игнорируем ошибки парсинга */ }

                if (timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs)
                    return;

                Thread.Sleep(50);
            }
        }
    }
}
