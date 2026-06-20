// AstroEditor.Core/Interpreter/AstroInterpreter.Conditions.cs
// Условные конструкции: IF, SWITCH, BREAK, CONTINUE

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    #region IF / ELSE / ENDIF

    [InstructionHandler("core.if")]
    private void ExecuteIf(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var exprNode = _parser.Parse(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (!Convert.ToBoolean(condition))
        {
            var endIndex = FindMatchingEndIf(_state.CurrentLineIndex);
            _state.CurrentLineIndex = endIndex;
        }
    }

    [InstructionHandler("core.else")]
    private void ExecuteElse(Instruction instruction)
    {
        var endIndex = FindMatchingEndIf(_state.CurrentLineIndex);
        _state.CurrentLineIndex = endIndex;
    }

    [InstructionHandler("core.endif")]
    private void ExecuteEndIf(Instruction instruction)
    {
        // Ничего не делаем
    }

    #endregion

    #region SWITCH / CASE / DEFAULT / ENDSWITCH

    [InstructionHandler("core.switch")]
    private void ExecuteSwitch(Instruction instruction)
    {
        var exprField = GetFieldValue<ExpressionFieldValue>(instruction, "expression");
        var exprNode = _parser.Parse(exprField.Expression);
        var evalContext = CreateExpressionContext();
        var value = _evaluator.Evaluate(exprNode, evalContext);

        var endIndex = FindMatchingEndSwitch(_state.CurrentLineIndex);
        var context = new SwitchContext
        {
            ExpressionValue = value,
            EndLineIndex = endIndex,
            IsExecuting = false
        };
        _state.SwitchStack.Push(context);

        var nextIndex = FindNextCaseOrDefault(_state.CurrentLineIndex + 1, value);
        _state.CurrentLineIndex = nextIndex;
    }

    [InstructionHandler("core.case")]
    private void ExecuteCase(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("Case outside switch");

        var context = _state.SwitchStack.Peek();

        if (context.IsExecuting)
            return;

        var valueField = GetFieldValue<ConstantFieldValue>(instruction, "value");
        var caseValue = valueField.Value;

        if (Equals(caseValue, context.ExpressionValue))
        {
            context.IsExecuting = true;
        }
        else
        {
            var nextIndex = FindNextCaseOrDefault(_state.CurrentLineIndex + 1, context.ExpressionValue);
            _state.CurrentLineIndex = nextIndex;
        }
    }

    [InstructionHandler("core.default")]
    private void ExecuteDefault(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("Default outside switch");

        var context = _state.SwitchStack.Peek();

        if (context.IsExecuting)
            return;

        context.IsExecuting = true;
    }

    [InstructionHandler("core.endswitch")]
    private void ExecuteEndSwitch(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("EndSwitch without matching Switch");

        var context = _state.SwitchStack.Pop();
        _state.CurrentLineIndex = context.EndLineIndex;
    }

    #endregion

    #region BREAK / CONTINUE

    [InstructionHandler("core.break")]
    private void ExecuteBreak(Instruction instruction)
    {
        if (_state.SwitchStack.Count > 0)
        {
            var context = _state.SwitchStack.Pop();
            _state.CurrentLineIndex = context.EndLineIndex;
            return;
        }

        if (_state.LoopStack.Count == 0)
            throw new Exception("Break outside loop or switch");

        var loop = _state.LoopStack.Pop();
        _state.CurrentLineIndex = loop.EndLineIndex;
    }

    [InstructionHandler("core.continue")]
    private void ExecuteContinue(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("Continue outside loop");

        var loop = _state.LoopStack.Peek();
        if (loop.IsForLoop)
        {
            _state.CurrentLineIndex = loop.EndLineIndex - 1;
        }
        else if (loop.IsForEachLoop)
        {
            _state.CurrentLineIndex = loop.EndLineIndex - 1;
        }
        else
        {
            _state.CurrentLineIndex = loop.StartLineIndex;
        }
    }

    #endregion
}
