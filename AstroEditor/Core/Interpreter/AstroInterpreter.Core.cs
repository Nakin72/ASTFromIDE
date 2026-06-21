// AstroEditor.Core/Interpreter/AstroInterpreter.Core.cs
// Базовые инструкции: присваивание, работа с переменными

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreterEx
{
    [InstructionHandler("core.assign")]
    private void ExecuteAssign(Instruction instruction)
    {
        try
        {
            var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
            var targetVar = FindVariable(varField.TableSetName, varField.VariableName);
            
            if (targetVar == null)
            {
                _logger.LogError("Variable '{VariableName}' not found in table set '{TableSetName}'", 
                    varField.VariableName, varField.TableSetName);
                throw new VariableNotFoundException(varField.VariableName, varField.TableSetName);
            }

            if (!instruction.Fields.TryGetValue("expression", out var valueField))
                throw new Exception("Field 'expression' not found in assign instruction");

            object? value;
            if (valueField is ExpressionFieldValue exprField)
            {
                // ✅ P1-6: Унифицированный вызов с кэшированием
                var exprNode = ParseCachedExpression(exprField.Expression);
                    
                var evalContext = CreateExpressionContext();
                value = _evaluator.Evaluate(exprNode, evalContext);
            }
            else if (valueField is ConstantFieldValue constField)
            {
                value = constField.Value;
            }
            else
            {
                _logger.LogError("Unsupported field type for expression: {FieldType}", valueField?.GetType());
                throw new Exception($"Unsupported field type for expression: {valueField?.GetType()}");
            }

            targetVar.Value = value;
            _logger.LogTrace("Assigned {VariableName} = {Value}", varField.VariableName, value);
        }
        catch (VariableNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute assign instruction at line {LineNumber}", instruction.LineNumber);
            throw;
        }
    }
}

/// <summary>
/// Исключение: переменная не найдена.
/// </summary>
public class VariableNotFoundException : Exception
{
    public string VariableName { get; }
    public string? TableSetName { get; }
    
    public VariableNotFoundException(string variableName, string? tableSetName = null)
        : base($"Variable '{variableName}' not found in {(tableSetName ?? "global tables")}")
    {
        VariableName = variableName;
        TableSetName = tableSetName;
    }
}
