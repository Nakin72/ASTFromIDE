using System.Text.Json.Serialization;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Types;

namespace AstroEditor.Core.Tables;

public class TypedVariableTable
{
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("variables")] public List<Variable> Variables { get; set; } = new();
    [JsonIgnore] public DataType? Type { get; set; }

    // ✅ P0-4: Lock для потокобезопасного доступа к Variables
    private readonly object _lock = new();

    public void AddVariable(Variable variable)
    {
        if (variable.TypeId != TypeId)
            throw new InvalidOperationException($"Тип переменной {variable.TypeId} не соответствует таблице {TypeId}");
        
        lock (_lock) // ✅ P0-4
        {
            Variables.Add(variable);
        }
    }
    
    public Variable? FindVariable(string name)
    {
        lock (_lock) // ✅ P0-4
        {
            return Variables.FirstOrDefault(v => v.Name == name);
        }
    }
    
    public bool RemoveVariable(string name)
    {
        lock (_lock) // ✅ P0-4
        {
            var found = FindVariable(name);
            if (found != null)
            {
                Variables.Remove(found);
                return true;
            }
            return false;
        }
    }
    
    public void Clear()
    {
        lock (_lock) // ✅ P0-4
        {
            Variables.Clear();
        }
    }
}