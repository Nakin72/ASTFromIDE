// AstroEditor.Core/Interpreter/AstroInterpreter.Loops.cs
// Циклы: WHILE, FOR, FOREACH и их обработчики

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreterEx
{
    #region WHILE

    [InstructionHandler("core.while")]
    private void ExecuteWhile(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var maxIterField = TryGetFieldValue<ConstantFieldValue>(instruction, "maxIterations", out var maxIterVal) ? maxIterVal : null;

        // ✅ P1-6: Используем кэш выражений
        var exprNode = ParseCachedExpression(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (!Convert.ToBoolean(condition))
        {
            var endIndex = FindMatchingEndWhile(_state.CurrentLineIndex);
            _state.CurrentLineIndex = endIndex;
            return;
        }

        var maxIter = maxIterField != null ? Convert.ToInt32(maxIterField.Value) : 1000;
        var loopContext = new LoopContext
        {
            StartLineIndex = _state.CurrentLineIndex,
            EndLineIndex = FindMatchingEndWhile(_state.CurrentLineIndex),
            MaxIterations = maxIter,
            CurrentIteration = 0
        };
        _state.LoopStack.Push(loopContext);
        _state.CurrentLoopIteration = 0;
    }

    [InstructionHandler("core.endwhile")]
    private void ExecuteEndWhile(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("EndWhile without matching While");

        var loop = _state.LoopStack.Peek();
        loop.CurrentIteration++;
        _state.CurrentLoopIteration = loop.CurrentIteration;

        if (loop.CurrentIteration >= loop.MaxIterations)
        {
            _state.LoopStack.Pop();
            _state.CurrentLineIndex = loop.EndLineIndex;
            return;
        }

        _state.CurrentLineIndex = loop.StartLineIndex;
    }

    #endregion

    #region FOR

    [InstructionHandler("core.for")]
    private void ExecuteFor(Instruction instruction)
    {
        var existingLoop = _state.LoopStack.FirstOrDefault(l => l.IsForLoop && l.StartLineIndex == _state.CurrentLineIndex);
        if (existingLoop != null)
        {
            var counterVarExisting = FindVariable("", existingLoop.VariableName!);
            if (counterVarExisting == null)
                throw new Exception($"Counter variable '{existingLoop.VariableName}' not found");

            var currentVal = Convert.ToDouble(counterVarExisting.Value);
            var endValExisting = Convert.ToDouble(existingLoop.EndValue);
            var isIncreasingExisting = existingLoop.IsIncreasing;

            bool shouldContinue = isIncreasingExisting ? currentVal <= endValExisting : currentVal >= endValExisting;

            if (shouldContinue)
            {
                // Продолжаем выполнение тела
            }
            else
            {
                _state.LoopStack.Pop();
                _state.CurrentLineIndex = existingLoop.EndLineIndex;
            }
            return;
        }

        var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
        var startField = GetFieldValue<ExpressionFieldValue>(instruction, "start");
        var endField = GetFieldValue<ExpressionFieldValue>(instruction, "end");
        var stepField = TryGetFieldValue<ExpressionFieldValue>(instruction, "step", out var stepExpr) ? stepExpr : null;

        var counterVar = FindVariable(varField.TableSetName, varField.VariableName);
        if (counterVar == null)
            throw new Exception($"Variable '{varField.VariableName}' not found");

        var evalContext = CreateExpressionContext();
        // ✅ P1-6: Используем кэш выражений
        var startVal = _evaluator.Evaluate(ParseCachedExpression(startField.Expression), evalContext);
        var endVal = _evaluator.Evaluate(ParseCachedExpression(endField.Expression), evalContext);
        object? stepVal = stepField != null
            ? _evaluator.Evaluate(ParseCachedExpression(stepField.Expression), evalContext)
            : 1;

        if (!IsNumeric(startVal) || !IsNumeric(endVal) || !IsNumeric(stepVal))
            throw new Exception("FOR loop requires numeric values for start, end, and step");

        var startNum = Convert.ToDouble(startVal);
        var endNum = Convert.ToDouble(endVal);
        var stepNum = Convert.ToDouble(stepVal);
        if (stepNum == 0)
            throw new Exception("FOR loop step cannot be zero");

        bool isIncreasing = stepNum > 0;
        bool shouldEnter = isIncreasing ? startNum <= endNum : startNum >= endNum;
        if (!shouldEnter)
        {
            _state.CurrentLineIndex = FindMatchingEndFor(_state.CurrentLineIndex);
            return;
        }

        counterVar.Value = startVal;

        var loopContext = new LoopContext
        {
            StartLineIndex = _state.CurrentLineIndex,
            EndLineIndex = FindMatchingEndFor(_state.CurrentLineIndex),
            MaxIterations = 1000,
            CurrentIteration = 0,
            IsForLoop = true,
            VariableName = varField.VariableName,
            StartValue = startVal,
            EndValue = endVal,
            StepValue = stepVal,
            IsIncreasing = isIncreasing,
            CurrentValue = startVal
        };
        _state.LoopStack.Push(loopContext);
    }

    [InstructionHandler("core.endfor")]
    private void ExecuteEndFor(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("EndFor without matching For");

        var loop = _state.LoopStack.Peek();
        if (!loop.IsForLoop)
            throw new Exception("EndFor found without For");

        loop.CurrentIteration++;
        if (loop.CurrentIteration >= loop.MaxIterations)
        {
            _state.LoopStack.Pop();
            _state.CurrentLineIndex = loop.EndLineIndex;
            return;
        }

        var counterVar = FindVariable("", loop.VariableName!);
        if (counterVar == null)
            throw new Exception($"Counter variable '{loop.VariableName}' not found");

        var currentVal = Convert.ToDouble(counterVar.Value);
        var stepVal = Convert.ToDouble(loop.StepValue);
        var newVal = currentVal + stepVal;
        counterVar.Value = newVal;

        _state.CurrentLineIndex = loop.StartLineIndex;
    }

    #endregion

    #region FOREACH

    [InstructionHandler("core.foreach")]
    private void ExecuteForEach(Instruction instruction)
    {
        var existingLoop = _state.LoopStack.FirstOrDefault(l => l.IsForEachLoop && l.StartLineIndex == _state.CurrentLineIndex);
        if (existingLoop != null)
        {
            existingLoop.CurrentIndex++;
            
            if (existingLoop.CurrentIndex >= existingLoop.CollectionValues?.Count)
            {
                _state.LoopStack.Pop();
                _state.CurrentLineIndex = existingLoop.EndLineIndex;
            }
            else
            {
                var existingItemVar = FindVariable("", existingLoop.ItemVariableName!);
                if (existingItemVar == null)
                    throw new Exception($"Item variable '{existingLoop.ItemVariableName}' not found");
                
                existingItemVar.Value = existingLoop.CollectionValues[existingLoop.CurrentIndex];
            }
            return;
        }

        var itemVarField = GetFieldValue<VariableFieldValue>(instruction, "itemVariable");
        var collectionVarField = GetFieldValue<VariableFieldValue>(instruction, "collection");
        
        var collectionVar = FindVariable(collectionVarField.TableSetName, collectionVarField.VariableName);
        if (collectionVar == null)
            throw new Exception($"Collection variable '{collectionVarField.VariableName}' not found");
        
        var collectionValues = NormalizeArray(collectionVar.Value);
        if (collectionValues == null)
            throw new Exception($"Variable '{collectionVarField.VariableName}' is not an array");
        
        if (collectionValues.Count == 0)
        {
            _state.CurrentLineIndex = FindMatchingEndForEach(_state.CurrentLineIndex);
            return;
        }

        var itemVar = FindVariable("", itemVarField.VariableName);
        if (itemVar == null)
            throw new Exception($"Item variable '{itemVarField.VariableName}' not found");
        
        itemVar.Value = collectionValues[0];
        
        var loopContext = new LoopContext
        {
            StartLineIndex = _state.CurrentLineIndex,
            EndLineIndex = FindMatchingEndForEach(_state.CurrentLineIndex),
            MaxIterations = collectionValues.Count * 2,
            CurrentIteration = 0,
            IsForEachLoop = true,
            ItemVariableName = itemVarField.VariableName,
            CollectionVariableName = collectionVarField.VariableName,
            CollectionValues = collectionValues,
            CurrentIndex = 0
        };
        _state.LoopStack.Push(loopContext);
    }

    [InstructionHandler("core.endforeach")]
    private void ExecuteEndForEach(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("EndForEach without matching ForEach");
        
        var loop = _state.LoopStack.Peek();
        if (!loop.IsForEachLoop)
            throw new Exception("EndForEach found without ForEach");
        
        loop.CurrentIndex++;
        
        if (loop.CurrentIndex >= loop.CollectionValues?.Count)
        {
            _state.LoopStack.Pop();
            _state.CurrentLineIndex = loop.EndLineIndex;
        }
        else
        {
            var itemVar = FindVariable("", loop.ItemVariableName!);
            if (itemVar == null)
                throw new Exception($"Item variable '{loop.ItemVariableName}' not found");
            
            itemVar.Value = loop.CollectionValues[loop.CurrentIndex];
            _state.CurrentLineIndex = loop.StartLineIndex;
        }
    }
    
    #endregion

    #region Helpers

    private bool IsNumeric(object? value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private List<object?>? NormalizeArray(object? value)
    {
        if (value == null) return null;
        if (value is List<object?> list) return list;
        if (value is Array arr)
        {
            var result = new List<object?>();
            foreach (var item in arr)
                result.Add(item);
            return result;
        }
        if (value is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return json.EnumerateArray().Select(e =>
            {
                return e.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => e.GetString(),
                    System.Text.Json.JsonValueKind.Number => e.TryGetInt32(out var i) ? (object?)i : e.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    _ => e.GetRawText()
                };
            }).ToList();
        }
        return null;
    }

    #endregion
}
