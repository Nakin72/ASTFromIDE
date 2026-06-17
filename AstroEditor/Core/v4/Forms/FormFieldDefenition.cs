using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Common;
using AstroEditor.Core.v4.Programs;

namespace AstroEditor.Core.v4.Forms;

public class FormFieldDefinition
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("valueType")] public FieldValueType ValueType { get; set; }
    [JsonPropertyName("allowedTypeIds")] public List<string> AllowedTypeIds { get; set; } = new();
    [JsonPropertyName("options")] public List<string>? Options { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
    [JsonPropertyName("defaultValue")] public FieldValue? DefaultValue { get; set; }
    [JsonPropertyName("constraints")] public ValueConstraints? Constraints { get; set; }
}