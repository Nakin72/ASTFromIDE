// AstroEditor.Core.v4/Expressions/Operators.cs

namespace AstroEditor.Core.v4.Expressions;

public enum BinaryOperator
{
    // Арифметические
    Add,        // +
    Subtract,   // -
    Multiply,   // *
    Divide,     // /
    Modulo,     // %
    Power,      // ^
    // Сравнения
    Equal,              // ==
    NotEqual,           // !=
    LessThan,           // <
    GreaterThan,        // >
    LessOrEqual,        // <=
    GreaterOrEqual,     // >=
    // Логические
    And,                // &&
    Or,                 // ||
    // Для структур можно добавить специальные (например, Concat для строк)
}

public enum UnaryOperator
{
    Plus,       // + (унарный)
    Minus,      // - (унарный)
    Not,        // !
}