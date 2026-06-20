// AstroEditor.Core/Interpreter/AstroInterpreter.Helpers.cs
// Вспомогательные методы для работы с полями, переменными и выражениями

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Variables;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    private T GetFieldValue<T>(Instruction instruction, string fieldName) where T : FieldValue
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && field is T typed)
            return typed;
        throw new Exception($"Field '{fieldName}' not found or not of type {typeof(T).Name}");
    }

    private bool TryGetFieldValue<T>(Instruction instruction, string fieldName, out T? value) where T : FieldValue
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && field is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    private Variable? FindVariable(string tableSetName, string variableName)
    {
        if (tableSetName == "Global")
            return _context.GlobalTables.FindVariable(variableName);
        else if (tableSetName == "LocalVariables" || tableSetName == _state.Program.Name + "_Local")
            return _state.CurrentLocalTables.FindVariable(variableName);
        else
        {
            var found = _state.CurrentLocalTables.FindVariable(variableName);
            if (found != null) return found;
            return _context.GlobalTables.FindVariable(variableName);
        }
    }

    private ExpressionContext CreateExpressionContext()
    {
        return new ExpressionContext
        {
            GlobalTables = _context.GlobalTables,
            LocalTables = _state.CurrentLocalTables,
            TypeRegistry = _context.TypeRegistry,
            Functions = _context.Functions
        };
    }

    private int FindMatchingEnd(int startIndex, string openFormId, string closeFormId)
    {
        int depth = 0;
        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            if (instr.FormId == openFormId)
                depth++;
            else if (instr.FormId == closeFormId)
            {
                depth--;
                if (depth == 0)
                    return i + 1;
            }
        }
        throw new Exception($"No matching '{closeFormId}' found for '{openFormId}'");
    }

    // Обёртки для поиска соответствующих закрывающих инструкций
    private int FindMatchingEndIf(int startIndex) => FindMatchingEnd(startIndex, "core.if", "core.endif");
    private int FindMatchingEndSwitch(int startIndex) => FindMatchingEnd(startIndex, "core.switch", "core.endswitch");
    private int FindMatchingEndFor(int startIndex) => FindMatchingEnd(startIndex, "core.for", "core.endfor");
    private int FindMatchingEndWhile(int startIndex) => FindMatchingEnd(startIndex, "core.while", "core.endwhile");
    private int FindMatchingEndForEach(int startIndex) => FindMatchingEnd(startIndex, "core.foreach", "core.endforeach");

    private int FindNextCaseOrDefault(int startIndex, object? switchValue)
    {
        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            if (instr.FormId == "core.case")
            {
                var valField = GetFieldValue<ConstantFieldValue>(instr, "value");
                if (Equals(valField.Value, switchValue))
                    return i;
            }
            else if (instr.FormId == "core.default")
            {
                return i;
            }
            else if (instr.FormId == "core.endswitch")
            {
                return i;
            }
        }
        throw new Exception("No matching case/default/endswitch found");
    }
}
