using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Types;

[JsonDerivedType(typeof(PrimitiveDataType), typeDiscriminator: "primitive")]
[JsonDerivedType(typeof(StructDataType), typeDiscriminator: "struct")]
[JsonDerivedType(typeof(ArrayDataType), typeDiscriminator: "array")]
[JsonDerivedType(typeof(AliasDataType), typeDiscriminator: "alias")]

public abstract class DataType
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("category")] public DataTypeCategory Category { get; set; } = DataTypeCategory.User;
    [JsonPropertyName("kind")] public abstract DataTypeKind Kind { get; }
    [JsonPropertyName("baseTypeId")] public string? BaseTypeId { get; set; }
    [JsonPropertyName("fields")] public List<StructField>? Fields { get; set; }
    [JsonPropertyName("arraySize")] public int? ArraySize { get; set; }
    [JsonPropertyName("constraints")] public ValueConstraints? Constraints { get; set; }
    [JsonPropertyName("operatorMethods")] public Dictionary<string, string>? OperatorMethods { get; set; }
    [JsonIgnore] public DataType? BaseType { get; set; }
}

public class StructField
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    [JsonIgnore] public DataType? Type { get; set; }
}
