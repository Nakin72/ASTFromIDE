// AstroEditor.Core.v4/Expressions/ExpressionEvaluator.cs
using System.Collections;
using AstroEditor.Core.v4.Types;

namespace AstroEditor.Core.v4.Expressions;

public class ExpressionEvaluator
{
    // AstroEditor.Core.v4/Expressions/ExpressionEvaluator.cs

    private object? NormalizeValue(object? value)
    {
        if (value is System.Text.Json.JsonElement jsonElement)
            return ConvertJsonElement(jsonElement);
        return value;
    }

    private object? ConvertJsonElement(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                return element.GetString();
            case System.Text.Json.JsonValueKind.Number:
                if (element.TryGetInt32(out int intVal))
                    return intVal;
                if (element.TryGetDouble(out double dblVal))
                    return dblVal;
                return element.GetRawText();
            case System.Text.Json.JsonValueKind.True:
                return true;
            case System.Text.Json.JsonValueKind.False:
                return false;
            case System.Text.Json.JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return dict;
            case System.Text.Json.JsonValueKind.Array:
                return element.EnumerateArray().Select(e => ConvertJsonElement(e)).ToList();
            case System.Text.Json.JsonValueKind.Null:
                return null;
            default:
                return element.GetRawText();
        }
    }
    public object? Evaluate(ExpressionNode node, ExpressionContext context)
    {
        return node switch
        {
            ConstantNode c => c.Value,
            VariableNode v => EvaluateVariable(v, context),
            FieldAccessNode f => EvaluateFieldAccess(f, context),
            IndexAccessNode i => EvaluateIndexAccess(i, context),
            FunctionCallNode f => EvaluateFunctionCall(f, context),
            BinaryExpressionNode b => EvaluateBinary(b, context),
            UnaryExpressionNode u => EvaluateUnary(u, context),
            TernaryExpressionNode t => EvaluateTernary(t, context),
            _ => throw new NotSupportedException($"Unknown expression node: {node.GetType()}")
        };
    }

    private object? EvaluateVariable(VariableNode v, ExpressionContext context)
    {
        var varObj = context.FindVariable(v.Name);
        if (varObj == null)
            throw new Exception($"Variable '{v.Name}' not found");
        return NormalizeValue(varObj.Value);
    }
    private object? EvaluateFieldAccess(FieldAccessNode f, ExpressionContext context)
    {
        var target = NormalizeValue(Evaluate(f.Target, context));
        if (target is IDictionary<string, object> dict)
        {
            if (dict.TryGetValue(f.FieldName, out var value))
                return NormalizeValue(value);
            throw new Exception($"Field '{f.FieldName}' not found in structure");
        }
        throw new Exception($"Cannot access field on non-structure");
    }

    private object? EvaluateIndexAccess(IndexAccessNode i, ExpressionContext context)
    {
        var target = NormalizeValue(Evaluate(i.Target, context));
        var index = NormalizeValue(Evaluate(i.Index, context));
        if (target is IList list && index is int idx)
        {
            if (idx >= 0 && idx < list.Count)
                return NormalizeValue(list[idx]);
            throw new Exception($"Index {idx} out of range");
        }
        if (target is Array arr && index is int idx2)
        {
            if (idx2 >= 0 && idx2 < arr.Length)
                return NormalizeValue(arr.GetValue(idx2));
            throw new Exception($"Index {idx2} out of range");
        }
        throw new Exception($"Cannot index non-array");
    }

    private object? EvaluateFunctionCall(FunctionCallNode f, ExpressionContext context)
    {
        var args = f.Arguments.Select(a => Evaluate(a, context)).ToArray();
        if (context.Functions.TryGetValue(f.FunctionName, out var func))
            return func(args);
        throw new Exception($"Function '{f.FunctionName}' not found");
    }

    private object? EvaluateBinary(BinaryExpressionNode b, ExpressionContext context)
    {
        var leftRaw = Evaluate(b.Left, context);
        var rightRaw = Evaluate(b.Right, context);
        var left = NormalizeValue(leftRaw);
        var right = NormalizeValue(rightRaw);

        return b.Operator switch
        {
            BinaryOperator.Add => Add(left, right, context),
            BinaryOperator.Subtract => Subtract(left, right),
            BinaryOperator.Multiply => Multiply(left, right),
            BinaryOperator.Divide => Divide(left, right),
            BinaryOperator.Modulo => Modulo(left, right),
            BinaryOperator.Power => Power(left, right),
            BinaryOperator.Equal => Equals(left, right),
            BinaryOperator.NotEqual => !Equals(left, right),
            BinaryOperator.LessThan => Compare(left, right) < 0,
            BinaryOperator.GreaterThan => Compare(left, right) > 0,
            BinaryOperator.LessOrEqual => Compare(left, right) <= 0,
            BinaryOperator.GreaterOrEqual => Compare(left, right) >= 0,
            BinaryOperator.And => And(left, right),
            BinaryOperator.Or => Or(left, right),
            _ => throw new Exception($"Unknown binary operator {b.Operator}")
        };
    }

    private object? EvaluateUnary(UnaryExpressionNode u, ExpressionContext context)
    {
        var operandRaw = Evaluate(u.Operand, context);
        var operand = NormalizeValue(operandRaw);

        return u.Operator switch
        {
            UnaryOperator.Plus => operand, // унарный плюс ничего не делает
            UnaryOperator.Minus => -Convert.ToDouble(operand),
            UnaryOperator.Not => !Convert.ToBoolean(operand),
            _ => throw new Exception($"Unknown unary operator {u.Operator}")
        };
    }

    private object? EvaluateTernary(TernaryExpressionNode t, ExpressionContext context)
    {
        var condRaw = Evaluate(t.Condition, context);
        var cond = NormalizeValue(condRaw);
        var condition = Convert.ToBoolean(cond);

        if (condition)
            return Evaluate(t.TrueExpression, context);
        else
            return Evaluate(t.FalseExpression, context);
    }

    #region Операции

    private object? Add(object? left, object? right, ExpressionContext context)
    {
        // Приводим к нормализованному виду (на случай, если это JsonElement)
        left = NormalizeValue(left);
        right = NormalizeValue(right);

        // Конкатенация строк
        if (left is string s1 && right is string s2)
            return s1 + s2;
        if (left is string s3)
            return s3 + Convert.ToString(right);
        if (right is string s4)
            return Convert.ToString(left) + s4;

        // Числовые типы (int, double, float, decimal и т.д.)
        if (left is IConvertible && right is IConvertible)
        {
            // Для универсальности приводим к double (можно улучшить, сохраняя исходный тип)
            return Convert.ToDouble(left) + Convert.ToDouble(right);
        }

        // Поддержка структур: если оба операнда — структуры с одинаковым типом,
        // пытаемся вызвать пользовательский оператор operator_Add.
        // Получаем тип левого операнда
        var leftType = GetType(left, context);
        var rightType = GetType(right, context);
        if (leftType != null && rightType != null && leftType.Id == rightType.Id)
        {
            var methodName = "operator_Add";
            if (TryCallTypeOperator(leftType, methodName, left, right, context, out var result))
                return result;
        }

        throw new Exception($"Cannot apply + to {left?.GetType()} and {right?.GetType()}");
    }

    private object? Subtract(object? left, object? right)
    {
        return Convert.ToDouble(left) - Convert.ToDouble(right);
    }

    private object? Multiply(object? left, object? right)
    {
        return Convert.ToDouble(left) * Convert.ToDouble(right);
    }

    private object? Divide(object? left, object? right)
    {
        var divisor = Convert.ToDouble(right);
        if (divisor == 0) throw new DivideByZeroException();
        return Convert.ToDouble(left) / divisor;
    }

    private object? Modulo(object? left, object? right)
    {
        return Convert.ToDouble(left) % Convert.ToDouble(right);
    }

    private object? Power(object? left, object? right)
    {
        return Math.Pow(Convert.ToDouble(left), Convert.ToDouble(right));
    }

    private bool And(object? left, object? right)
    {
        return Convert.ToBoolean(left) && Convert.ToBoolean(right);
    }

    private bool Or(object? left, object? right)
    {
        return Convert.ToBoolean(left) || Convert.ToBoolean(right);
    }

    // private int Compare(object? left, object? right)
    // {
    //     if (left is IComparable cmp && right is IComparable)
    //         return cmp.CompareTo(right);
    //     if (left is string str && right is string str2)
    //         return string.Compare(str, str2);
    //     throw new Exception($"Cannot compare {left?.GetType()} and {right?.GetType()}");
    // }

    private int Compare(object? left, object? right)
    {
        // Приводим к нормализованному виду (на случай JsonElement)
        left = NormalizeValue(left);
        right = NormalizeValue(right);

        // Если оба — числа, приводим к double и сравниваем
        if (left is IConvertible && right is IConvertible)
        {
            try
            {
                var d1 = Convert.ToDouble(left);
                var d2 = Convert.ToDouble(right);
                return d1.CompareTo(d2);
            }
            catch
            {
                // Не удалось привести — пробуем другие способы
            }
        }

        // Строки
        if (left is string str1 && right is string str2)
            return string.Compare(str1, str2);

        // Другие IComparable (если типы совместимы)
        if (left is IComparable cmp && right is IComparable)
            return cmp.CompareTo(right);

        throw new Exception($"Cannot compare {left?.GetType()} and {right?.GetType()}");
    }

    #endregion

    #region Вспомогательные методы для типов

    private DataType? GetType(object? obj, ExpressionContext context)
    {
        // Если объект — структура, пытаемся определить тип по значению.
        // В нашем случае структуры хранятся как Dictionary<string, object>.
        if (obj is IDictionary<string, object>)
        {
            // Пока не можем определить точный тип без дополнительной информации.
            // Можно вернуть null, или попробовать найти тип, у которого поля совпадают.
            // Для простоты возвращаем null.
            return null;
        }
        // Для примитивов можно попробовать найти тип по значению,
        // но это сложно, поэтому оставим заглушку.
        return null;
    }
    private bool TryCallTypeOperator(DataType type, string methodName, object? left, object? right, ExpressionContext context, out object? result)
    {
        result = null;
        // Здесь мы должны найти метод типа (оператор), который реализован как функция.
        // Например, в типе есть поле "OperatorMethods" с именем метода в реестре функций.
        if (type is StructDataType structType && structType.OperatorMethods != null && structType.OperatorMethods.TryGetValue(methodName, out var funcName))
        {
            if (context.Functions.TryGetValue(funcName, out var func))
            {
                result = func(new[] { left, right });
                return true;
            }
        }
        return false;
    }

    #endregion

}