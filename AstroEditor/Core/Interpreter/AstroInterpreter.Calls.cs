// AstroEditor.Core/Interpreter/AstroInterpreter.Calls.cs
// Вызовы программ, переходы, возвраты

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Variables;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreterEx
{
    #region CALL / RETURN

    [InstructionHandler("core.call")]
    private void ExecuteCall(Instruction instruction)
    {
        var progNameField = GetFieldValue<ConstantFieldValue>(instruction, "programName");
        var progName = progNameField.Value?.ToString();
        if (string.IsNullOrEmpty(progName))
            throw new Exception("Call: program name is empty");

        if (!_context.ProgramRegistry.TryGetValue(progName, out var calledProgram))
            throw new Exception($"Program '{progName}' not found");

        var frame = new CallFrame
        {
            Program = _state.Program,
            ReturnLineIndex = _state.CurrentLineIndex + 1,
            LocalTables = _state.CurrentLocalTables,
            Arguments = new Dictionary<string, object?>()
        };

        if (instruction.Fields.TryGetValue("arguments", out var argsField) && argsField is ConstantFieldValue constArgs)
        {
            if (constArgs.Value is List<object> argsList)
            {
                for (int i = 0; i < Math.Min(argsList.Count, calledProgram.Arguments.Count); i++)
                {
                    var argName = calledProgram.Arguments[i].Name;
                    frame.Arguments[argName] = argsList[i];
                }
            }
        }

        var newLocalTables = new VariableTableSet
        {
            Name = calledProgram.Name + "_Local",
            IsGlobal = false,
            Tables = new System.Collections.Concurrent.ConcurrentDictionary<string, TypedVariableTable>()
        };

        foreach (var kv in calledProgram.LocalTables.Tables)
        {
            var newTable = new TypedVariableTable
            {
                TypeId = kv.Key,
                Type = kv.Value.Type,
                Variables = kv.Value.Variables.Select(v => new Variable
                {
                    Name = v.Name,
                    TypeId = v.TypeId,
                    Type = v.Type,
                    Value = v.Value
                }).ToList()
            };
            newLocalTables.Tables[kv.Key] = newTable;
        }

        foreach (var arg in calledProgram.Arguments)
        {
            if (frame.Arguments.TryGetValue(arg.Name, out var argValue))
            {
                var varObj = newLocalTables.FindVariable(arg.Name);
                if (varObj != null)
                    varObj.Value = argValue;
                else
                {
                    var type = _context.TypeRegistry.GetTypeById(arg.TypeId);
                    if (type != null)
                    {
                        var newVar = new Variable(arg.Name, type, argValue);
                        newLocalTables.AddVariable(newVar, _context.TypeRegistry);
                    }
                }
            }
        }

        _state.CallStack.Push(frame);
        _state.Program = calledProgram;
        _state.CurrentLocalTables = newLocalTables;
        _state.CurrentLineIndex = 0;
    }

    [InstructionHandler("core.return")]
    private void ExecuteReturn(Instruction instruction)
    {
        if (instruction.Fields.TryGetValue("value", out var valueField))
        {
            if (valueField is ExpressionFieldValue exprField)
            {
                // ✅ P1-6: Используем кэш выражений
                var exprNode = ParseCachedExpression(exprField.Expression);
                var evalContext = CreateExpressionContext();
                _state.ReturnValue = _evaluator.Evaluate(exprNode, evalContext);
            }
            else if (valueField is ConstantFieldValue constField)
            {
                _state.ReturnValue = constField.Value;
            }
        }

        if (_state.CallStack.Count > 0)
        {
            var frame = _state.CallStack.Pop();
            _state.Program = frame.Program;
            _state.CurrentLocalTables = frame.LocalTables;
            _state.CurrentLineIndex = frame.ReturnLineIndex;
        }
        else
        {
            _state.StopRequested = true;
        }
    }

    #endregion

    #region LABELS / JUMPS

    [InstructionHandler("core.lbl")]
    private void ExecuteLabel(Instruction instruction)
    {
        // Ничего не делаем - метки обрабатываются при загрузке программы
    }

    [InstructionHandler("core.jumplbl")]
    private void ExecuteJumpLbl(Instruction instruction)
    {
        var labelField = GetFieldValue<ConstantFieldValue>(instruction, "labelName");
        var labelName = labelField.Value?.ToString();
        if (string.IsNullOrEmpty(labelName))
            throw new Exception("JumpLbl: label name is empty");

        if (_state.Program.Labels.TryGetValue(labelName, out var lineNumber))
        {
            var index = _state.Program.Lines.FindIndex(l => l.LineNumber == lineNumber);
            if (index >= 0)
                _state.CurrentLineIndex = index;
            else
                throw new Exception($"Label '{labelName}' points to line {lineNumber} which does not exist");
        }
        else
        {
            throw new Exception($"Label '{labelName}' not found");
        }
    }

    [InstructionHandler("core.jumpif")]
    private void ExecuteJumpIf(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var labelField = GetFieldValue<ConstantFieldValue>(instruction, "labelName");

        // ✅ P1-6: Используем кэш выражений
        var exprNode = ParseCachedExpression(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (Convert.ToBoolean(condition))
        {
            var labelName = labelField.Value?.ToString();
            if (string.IsNullOrEmpty(labelName))
                throw new Exception("JumpIf: label name is empty");

            if (_state.Program.Labels.TryGetValue(labelName, out var lineNumber))
            {
                var index = _state.Program.Lines.FindIndex(l => l.LineNumber == lineNumber);
                if (index >= 0)
                    _state.CurrentLineIndex = index;
                else
                    throw new Exception($"Label '{labelName}' points to line {lineNumber} which does not exist");
            }
            else
            {
                throw new Exception($"Label '{labelName}' not found");
            }
        }
    }

    #endregion
}
