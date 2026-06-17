// AstroEditor.Core.v4/Expressions/ExecutionContext.cs
using AstroEditor.Core.v4.Tables;
using AstroEditor.Core.v4.Variables;
using AstroEditor.Core.v4.Types;

namespace AstroEditor.Core.v4.Expressions;

public class ExpressionContext
{
    public VariableTableSet? GlobalTables { get; set; }
    public VariableTableSet? LocalTables { get; set; }
    public DataTypeRegistry TypeRegistry { get; set; } = null!;
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; } = new();

    public Variable? FindVariable(string name)
    {
        // Сначала ищем в локальных, затем в глобальных
        var found = LocalTables?.FindVariable(name);
        if (found != null) return found;
        return GlobalTables?.FindVariable(name);
    }
}