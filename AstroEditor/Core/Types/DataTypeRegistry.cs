using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace AstroEditor.Core.Types;

/// <summary>
/// Реестр типов данных.
/// ✅ P1-1: Использует ConcurrentDictionary для потокобезопасности без блокировок.
/// </summary>
public class DataTypeRegistry
{
    [JsonIgnore]
    private readonly ConcurrentDictionary<string, DataType> _typesById = new();
    [JsonIgnore]
    private readonly ConcurrentDictionary<string, DataType> _typesByName = new();

    /// <summary>
    /// Сериализуемый список типов. Для десериализации используйте SetAllTypes.
    /// </summary>
    [JsonPropertyName("allTypes")]
    public List<DataType> AllTypesList
    {
        get
        {
            return _typesById.Values.ToList();
        }
        set
        {
            _typesById.Clear();
            _typesByName.Clear();
            if (value != null)
                foreach (var type in value)
                    RegisterType(type);
        }
    }

    [JsonIgnore]
    public IReadOnlyCollection<DataType> AllTypes
    {
        get
        {
            return _typesById.Values.ToList().AsReadOnly();
        }
    }

    public void RegisterType(DataType type)
    {
        if (string.IsNullOrEmpty(type.Id)) type.Id = Guid.NewGuid().ToString();
        _typesById[type.Id] = type;
        _typesByName[type.Name] = type;
    }

    public DataType? GetTypeById(string id)
    {
        return _typesById.GetValueOrDefault(id);
    }
    
    public DataType? GetTypeByName(string name)
    {
        return _typesByName.GetValueOrDefault(name);
    }

    public bool RemoveType(string id)
    {
        if (_typesById.TryGetValue(id, out var type))
        {
            _typesById.TryRemove(id, out _);
            _typesByName.TryRemove(type.Name, out _);
            return true;
        }
        return false;
    }
    
    public void Clear()
    {
        _typesById.Clear();
        _typesByName.Clear();
    }

    public void ResolveReferences()
    {
        foreach (var type in _typesById.Values.ToList())
        {
            if (!string.IsNullOrEmpty(type.BaseTypeId))
                type.BaseType = GetTypeById(type.BaseTypeId);
            if (type is StructDataType structType && structType.Fields != null)
            {
                foreach (var field in structType.Fields)
                    field.Type = GetTypeById(field.TypeId);
            }
        }
    }
}