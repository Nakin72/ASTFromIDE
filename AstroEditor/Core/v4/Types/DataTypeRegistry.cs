using System.Collections.ObjectModel;

namespace AstroEditor.Core.v4.Types;

public class DataTypeRegistry
{
    private readonly Dictionary<string, DataType> _typesById = new();
    private readonly Dictionary<string, DataType> _typesByName = new();

    public IReadOnlyCollection<DataType> AllTypes => _typesById.Values.ToList().AsReadOnly();

    public void RegisterType(DataType type)
    {
        if (string.IsNullOrEmpty(type.Id)) type.Id = Guid.NewGuid().ToString();
        _typesById[type.Id] = type;
        _typesByName[type.Name] = type;
    }

    public DataType? GetTypeById(string id) => _typesById.GetValueOrDefault(id);
    public DataType? GetTypeByName(string name) => _typesByName.GetValueOrDefault(name);

    public bool RemoveType(string id)
    {
        if (_typesById.TryGetValue(id, out var type))
        {
            _typesById.Remove(id);
            _typesByName.Remove(type.Name);
            return true;
        }
        return false;
    }
    public void Clear() { _typesById.Clear(); _typesByName.Clear(); }

    public void ResolveReferences()
    {
        foreach (var type in AllTypes)
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