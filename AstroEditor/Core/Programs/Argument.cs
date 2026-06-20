using System.Text.Json.Serialization;
using AstroEditor.Core.Types;
using AstroEditor.Core.Common;

namespace AstroEditor.Core.Programs;

public class Argument
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    [JsonPropertyName("direction")] public ArgumentDirection Direction { get; set; } = ArgumentDirection.In;
    [JsonPropertyName("defaultValue")] public object? DefaultValue { get; set; }
    [JsonIgnore] public DataType? Type { get; set; }
}