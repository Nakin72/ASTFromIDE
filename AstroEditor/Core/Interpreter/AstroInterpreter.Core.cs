// AstroEditor.Core/Interpreter/AstroInterpreter.Core.cs
// Базовые инструкции: присваивание, работа с переменными

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    [InstructionHandler("core.assign")]
    private void ExecuteAssign(Instruction instruction)
    {
        var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
        var targetVar = FindVariable(varField.TableSetName, varField.VariableName);
        if (targetVar == null)
            throw new Exception($"Variable '{varField.VariableName}' not found");

        if (!instruction.Fields.TryGetValue("expression", out var valueField))
            throw new Exception("Field 'expression' not found");

        object? value;
        if (valueField is ExpressionFieldValue exprField)
        {
            var exprNode = _parser.Parse(exprField.Expression);
            var evalContext = CreateExpressionContext();
            value = _evaluator.Evaluate(exprNode, evalContext);
        }
        else if (valueField is ConstantFieldValue constField)
        {
            value = constField.Value;
        }
        else
        {
            throw new Exception($"Unsupported field type for expression: {valueField?.GetType()}");
        }

        targetVar.Value = value;
    }
}
