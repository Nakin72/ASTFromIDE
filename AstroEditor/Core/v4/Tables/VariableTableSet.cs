using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Types;
using AstroEditor.Core.v4.Variables;

namespace AstroEditor.Core.v4.Tables;

public class VariableTableSet
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("isGlobal")] public bool IsGlobal { get; set; }
    [JsonPropertyName("tables")] public Dictionary<string, TypedVariableTable> Tables { get; set; } = new();

    public TypedVariableTable GetOrCreateTable(DataType type)
    {
        if (!Tables.TryGetValue(type.Id, out var table))
        {
            table = new TypedVariableTable { TypeId = type.Id, Type = type };
            Tables[type.Id] = table;
        }
        return table;
    }

    public void AddVariable(Variable variable, DataTypeRegistry registry)
    {
        var type = registry.GetTypeById(variable.TypeId) ?? throw new Exception($"Тип {variable.TypeId} не найден");
        var table = GetOrCreateTable(type);
        table.AddVariable(variable);
    }

    public Variable? FindVariable(string name)
    {
        foreach (var table in Tables.Values)
        {
            var found = table.FindVariable(name);
            if (found != null) return found;
        }
        return null;
    }

    public Variable? FindVariable(string name, string typeId)
    {
        if (Tables.TryGetValue(typeId, out var table))
            return table.FindVariable(name);
        return null;
    }

    public IEnumerable<Variable> GetVariablesByType(string typeId)
    {
        if (Tables.TryGetValue(typeId, out var table))
            return table.Variables;
        return Enumerable.Empty<Variable>();
    }

    public IEnumerable<Variable> GetVariablesByCompatibleTypes(IEnumerable<string> typeIds, DataTypeRegistry registry)
    {
        var typeSet = new HashSet<string>(typeIds);
        var result = new List<Variable>();
        foreach (var kv in Tables)
        {
            var dataType = registry.GetTypeById(kv.Key);
            if (dataType == null) continue;
            if (IsTypeCompatible(dataType, typeSet, registry))
                result.AddRange(kv.Value.Variables);
        }
        return result;
    }

    private bool IsTypeCompatible(DataType type, HashSet<string> targetTypeIds, DataTypeRegistry registry)
    {
        if (targetTypeIds.Contains(type.Id)) return true;
        if (type is AliasDataType alias && alias.BaseType != null)
            return IsTypeCompatible(alias.BaseType, targetTypeIds, registry);
        return false;
    }

    public void ResolveReferences(DataTypeRegistry registry)
    {
        foreach (var kv in Tables)
        {
            var table = kv.Value;
            table.Type = registry.GetTypeById(table.TypeId);
            foreach (var variable in table.Variables)
                variable.Type = registry.GetTypeById(variable.TypeId);
        }
    }
}