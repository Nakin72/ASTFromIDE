using System.Text.Json.Serialization;

namespace AstroEditor.Core.v4.Common;

public class ValueConstraints
{
    [JsonPropertyName("min")] public double? Min { get; set; }
    [JsonPropertyName("max")] public double? Max { get; set; }
    [JsonPropertyName("allowedValues")] public List<object>? AllowedValues { get; set; }
    [JsonPropertyName("minLength")] public int? MinLength { get; set; }
    [JsonPropertyName("maxLength")] public int? MaxLength { get; set; }
}