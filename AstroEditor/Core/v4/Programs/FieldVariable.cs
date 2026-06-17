using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Programs;

[JsonDerivedType(typeof(ConstantFieldValue), typeDiscriminator: "constant")]
[JsonDerivedType(typeof(VariableFieldValue), typeDiscriminator: "variable")]
[JsonDerivedType(typeof(EnumFieldValue), typeDiscriminator: "enum")]
[JsonDerivedType(typeof(ExpressionFieldValue), typeDiscriminator: "expression")]
[JsonDerivedType(typeof(FunctionCallFieldValue), typeDiscriminator: "functioncall")]
[JsonDerivedType(typeof(LabelFieldValue), typeDiscriminator: "label")]
public abstract class FieldValue
{
    [JsonPropertyName("type")] public abstract string Type { get; }
}

public class ConstantFieldValue : FieldValue
{
    public override string Type => "constant";
    [JsonPropertyName("value")] public object? Value { get; set; }
    public ConstantFieldValue() { }
    public ConstantFieldValue(object value) => Value = value;
}

public class VariableFieldValue : FieldValue
{
    public override string Type => "variable";
    [JsonPropertyName("tableSetName")] public string TableSetName { get; set; } = string.Empty;
    [JsonPropertyName("variableName")] public string VariableName { get; set; } = string.Empty;
    [JsonPropertyName("typeId")] public string TypeId { get; set; } = string.Empty;
    public VariableFieldValue() { }
    public VariableFieldValue(string tableSetName, string variableName, string typeId)
    {
        TableSetName = tableSetName;
        VariableName = variableName;
        TypeId = typeId;
    }
}

public class EnumFieldValue : FieldValue
{
    public override string Type => "enum";
    [JsonPropertyName("selectedValue")] public string SelectedValue { get; set; } = string.Empty;
    [JsonPropertyName("options")] public List<string> Options { get; set; } = new();
    public EnumFieldValue() { }
    public EnumFieldValue(string selected, List<string> options)
    {
        SelectedValue = selected;
        Options = options;
    }
}

public class ExpressionFieldValue : FieldValue
{
    public override string Type => "expression";
    [JsonPropertyName("expression")] public string Expression { get; set; } = string.Empty;
    public ExpressionFieldValue() { }
    public ExpressionFieldValue(string expression) => Expression = expression;
}

public class FunctionCallFieldValue : FieldValue
{
    public override string Type => "functioncall";
    [JsonPropertyName("functionName")] public string FunctionName { get; set; } = string.Empty;
    [JsonPropertyName("arguments")] public List<FieldValue> Arguments { get; set; } = new();
    public FunctionCallFieldValue() { }
    public FunctionCallFieldValue(string functionName, List<FieldValue>? args = null)
    {
        FunctionName = functionName;
        if (args != null) Arguments = args;
    }
}

public class LabelFieldValue : FieldValue
{
    public override string Type => "label";
    [JsonPropertyName("labelName")] public string LabelName { get; set; } = string.Empty;
    public LabelFieldValue() { }
    public LabelFieldValue(string labelName) => LabelName = labelName;
}