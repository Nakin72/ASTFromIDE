// AstroEditor.Core.v4/Expressions/ExpressionNode.cs
using System.Text.Json.Serialization;

namespace AstroEditor.Core.v4.Expressions;

[JsonDerivedType(typeof(ConstantNode), typeDiscriminator: "constant")]
[JsonDerivedType(typeof(VariableNode), typeDiscriminator: "variable")]
[JsonDerivedType(typeof(FieldAccessNode), typeDiscriminator: "fieldaccess")]
[JsonDerivedType(typeof(FunctionCallNode), typeDiscriminator: "functioncall")]
[JsonDerivedType(typeof(BinaryExpressionNode), typeDiscriminator: "binary")]
[JsonDerivedType(typeof(UnaryExpressionNode), typeDiscriminator: "unary")]
[JsonDerivedType(typeof(TernaryExpressionNode), typeDiscriminator: "ternary")]
public abstract class ExpressionNode
{
    // Базовый класс, никаких свойств, только маркер для полиморфизма
}

// --- Конкретные узлы ---

public class ConstantNode : ExpressionNode
{
    [JsonPropertyName("value")] public object? Value { get; set; }
    public ConstantNode() { }
    public ConstantNode(object value) => Value = value;
}

public class VariableNode : ExpressionNode
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("tableSetName")] public string? TableSetName { get; set; } // если null - искать в глобальных и локальных
    public VariableNode() { }
    public VariableNode(string name, string? tableSetName = null) { Name = name; TableSetName = tableSetName; }
}

public class FieldAccessNode : ExpressionNode
{
    [JsonPropertyName("target")] public ExpressionNode Target { get; set; } = null!;
    [JsonPropertyName("fieldName")] public string FieldName { get; set; } = string.Empty;
    public FieldAccessNode() { }
    public FieldAccessNode(ExpressionNode target, string fieldName) { Target = target; FieldName = fieldName; }
}

public class FunctionCallNode : ExpressionNode
{
    [JsonPropertyName("functionName")] public string FunctionName { get; set; } = string.Empty;
    [JsonPropertyName("arguments")] public List<ExpressionNode> Arguments { get; set; } = new();
    public FunctionCallNode() { }
    public FunctionCallNode(string functionName, List<ExpressionNode>? args = null)
    {
        FunctionName = functionName;
        if (args != null) Arguments = args;
    }
}

public class BinaryExpressionNode : ExpressionNode
{
    [JsonPropertyName("operator")] public BinaryOperator Operator { get; set; }
    [JsonPropertyName("left")] public ExpressionNode Left { get; set; } = null!;
    [JsonPropertyName("right")] public ExpressionNode Right { get; set; } = null!;
    public BinaryExpressionNode() { }
    public BinaryExpressionNode(BinaryOperator op, ExpressionNode left, ExpressionNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }
}

public class UnaryExpressionNode : ExpressionNode
{
    [JsonPropertyName("operator")] public UnaryOperator Operator { get; set; }
    [JsonPropertyName("operand")] public ExpressionNode Operand { get; set; } = null!;
    public UnaryExpressionNode() { }
    public UnaryExpressionNode(UnaryOperator op, ExpressionNode operand)
    {
        Operator = op;
        Operand = operand;
    }
}

public class TernaryExpressionNode : ExpressionNode
{
    [JsonPropertyName("condition")] public ExpressionNode Condition { get; set; } = null!;
    [JsonPropertyName("trueExpression")] public ExpressionNode TrueExpression { get; set; } = null!;
    [JsonPropertyName("falseExpression")] public ExpressionNode FalseExpression { get; set; } = null!;
    public TernaryExpressionNode() { }
    public TernaryExpressionNode(ExpressionNode condition, ExpressionNode trueExpr, ExpressionNode falseExpr)
    {
        Condition = condition;
        TrueExpression = trueExpr;
        FalseExpression = falseExpr;
    }
}
[JsonDerivedType(typeof(IndexAccessNode), typeDiscriminator: "indexaccess")]
public class IndexAccessNode : ExpressionNode
{
    [JsonPropertyName("target")] public ExpressionNode Target { get; set; } = null!;
    [JsonPropertyName("index")] public ExpressionNode Index { get; set; } = null!;
    public IndexAccessNode() { }
    public IndexAccessNode(ExpressionNode target, ExpressionNode index)
    {
        Target = target;
        Index = index;
    }
}