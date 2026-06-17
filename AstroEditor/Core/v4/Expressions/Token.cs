// AstroEditor.Core.v4/Expressions/Token.cs
namespace AstroEditor.Core.v4.Expressions;

public enum TokenType
{
    Number, String, Bool, Identifier,
    Plus, Minus, Multiply, Divide, Modulo, Power,
    Equal, NotEqual, Less, Greater, LessOrEqual, GreaterOrEqual,
    And, Or, Not,
    LeftParen, RightParen, LeftBracket, RightBracket, Dot, Comma,
    Question, Colon,
    EndOfFile
}

public class Token
{
    public TokenType Type { get; }
    public string? Text { get; }
    public int Position { get; }
    public object? Literal { get; }

    public Token(TokenType type, string? text, int position, object? literal = null)
    {
        Type = type;
        Text = text;
        Position = position;
        Literal = literal;
    }

    public override string ToString() => $"{Type}: '{Text}' (pos {Position})";
}