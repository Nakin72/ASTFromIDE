using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Types;
using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Programs;

public class Argument
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("direction")] public ArgumentDirection Direction { get; set; } = ArgumentDirection.In;
    [JsonPropertyName("defaultValue")] public object? DefaultValue { get; set; }
    [JsonIgnore] public DataType? Type { get; set; }
}