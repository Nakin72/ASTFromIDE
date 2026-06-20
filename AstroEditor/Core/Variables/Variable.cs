using System.Text.Json.Serialization;
using AstroEditor.Core.Common;
using AstroEditor.Core.Types;

namespace AstroEditor.Core.Variables;

public class Variable
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("value")] public object? Value { get; set; }
    [JsonPropertyName("comment")] public string? Comment { get; set; }
    [JsonPropertyName("constraints")] public ValueConstraints? Constraints { get; set; }
    [JsonIgnore] public DataType? Type { get; set; }

    public Variable() { }
    public Variable(string name, DataType type, object? value = null)
    {
        Name = name;
        Type = type;
        TypeId = type.Id;
        Value = value;
    }
}