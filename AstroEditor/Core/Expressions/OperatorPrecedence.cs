// AstroEditor.Core.v4/Expressions/OperatorPrecedence.cs

namespace AstroEditor.Core.Expressions;

public static class OperatorPrecedence
{
    public static int GetPrecedence(BinaryOperator op)
    {
        return op switch
        {
            // Наивысший приоритет
            BinaryOperator.Power => 5,
            // Умножение, деление, остаток
            BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo => 4,
            // Сложение, вычитание
            BinaryOperator.Add or BinaryOperator.Subtract => 3,
            // Сравнения
            BinaryOperator.LessThan or BinaryOperator.GreaterThan or BinaryOperator.LessOrEqual or BinaryOperator.GreaterOrEqual => 2,
            // Равенство/неравенство
            BinaryOperator.Equal or BinaryOperator.NotEqual => 1,
            // Логические (низший приоритет)
            BinaryOperator.And => 0,
            BinaryOperator.Or => -1,
            _ => 0
        };
    }

    public static bool IsLeftAssociative(BinaryOperator op)
    {
        // Все операторы левоассоциативны, кроме возведения в степень (можно сделать правоассоциативным)
        return op != BinaryOperator.Power; // Power можно сделать правоассоциативным
    }
}