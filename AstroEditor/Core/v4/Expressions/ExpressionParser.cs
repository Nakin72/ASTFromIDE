// AstroEditor.Core.v4/Expressions/ExpressionParser.cs
using System.Text;

namespace AstroEditor.Core.v4.Expressions;

public class ExpressionParser
{
    private List<Token> _tokens = new();
    private int _pos = 0;
    private Token _current => _pos < _tokens.Count ? _tokens[_pos] : null!;

    public ExpressionNode Parse(string input)
    {
        _tokens = Lexer.Tokenize(input);
        _pos = 0;
        var expr = ParseExpression();
        if (_current.Type != TokenType.EndOfFile)
            throw new Exception($"Unexpected token '{_current.Text}' at position {_current.Position}");
        return expr;
    }

    private ExpressionNode ParseExpression(int precedence = 0)
    {
        var left = ParsePrimary();

        while (true)
        {
            var op = _current.Type;
            if (!IsBinaryOperator(op)) break;

            var opPrec = GetPrecedence(op);
            if (opPrec < precedence) break;

            // Тернарный оператор ?
            if (op == TokenType.Question)
            {
                Consume(TokenType.Question);
                var trueExpr = ParseExpression();
                Consume(TokenType.Colon);
                var falseExpr = ParseExpression();
                left = new TernaryExpressionNode(left, trueExpr, falseExpr);
                continue;
            }

            var binOp = MapBinaryOperator(op);
            Consume(op);
            var right = ParseExpression(opPrec + 1);
            left = new BinaryExpressionNode(binOp, left, right);
        }

        return left;
    }

    private ExpressionNode ParsePrimary()
    {
        var token = _current;

        // Константы
        if (token.Type == TokenType.Number || token.Type == TokenType.String || token.Type == TokenType.Bool)
        {
            Consume();
            return new ConstantNode(token.Literal!);
        }

        // Идентификатор (переменная, функция, доступ к полям/индексам)
        if (token.Type == TokenType.Identifier)
        {
            var name = token.Text!;
            Consume();

            // Вызов функции: f(...)
            if (_current.Type == TokenType.LeftParen)
            {
                Consume(TokenType.LeftParen);
                var args = new List<ExpressionNode>();
                if (_current.Type != TokenType.RightParen)
                {
                    // Первый аргумент
                    args.Add(ParseExpression());
                    // Последующие аргументы через запятую
                    while (_current.Type == TokenType.Comma)
                    {
                        Consume(TokenType.Comma);
                        args.Add(ParseExpression());
                    }
                }
                Consume(TokenType.RightParen);
                return new FunctionCallNode(name, args);
            }

            // Переменная, возможно с доступами
            ExpressionNode left = new VariableNode(name);

            // Доступ к полю: variable.field
            while (_current.Type == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                var fieldName = Consume(TokenType.Identifier).Text!;
                left = new FieldAccessNode(left, fieldName);
            }

            // Доступ по индексу: array[0]
            if (_current.Type == TokenType.LeftBracket)
            {
                Consume(TokenType.LeftBracket);
                var index = ParseExpression();
                Consume(TokenType.RightBracket);
                left = new IndexAccessNode(left, index);
            }

            return left;
        }

        // Унарный оператор
        if (IsUnaryOperator(token.Type))
        {
            var op = MapUnaryOperator(token.Type);
            Consume();
            var operand = ParsePrimary();
            return new UnaryExpressionNode(op, operand);
        }

        // Группировка: ( ... )
        if (token.Type == TokenType.LeftParen)
        {
            Consume(TokenType.LeftParen);
            var expr = ParseExpression();
            Consume(TokenType.RightParen);
            return expr;
        }

        throw new Exception($"Unexpected token '{token.Text}' at position {token.Position}");
    }

    #region Вспомогательные методы

    private Token Consume(TokenType expectedType)
    {
        if (_current.Type == expectedType)
        {
            var t = _current;
            _pos++;
            return t;
        }
        throw new Exception($"Expected {expectedType}, got {_current.Type} at position {_current.Position}");
    }

    private void Consume() => _pos++;

    private bool IsBinaryOperator(TokenType type)
    {
        return type switch
        {
            TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or TokenType.Modulo or TokenType.Power or
            TokenType.Equal or TokenType.NotEqual or TokenType.Less or TokenType.Greater or TokenType.LessOrEqual or TokenType.GreaterOrEqual or
            TokenType.And or TokenType.Or or TokenType.Question => true,
            _ => false
        };
    }

    private bool IsUnaryOperator(TokenType type) => type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Not;

    private int GetPrecedence(TokenType op)
    {
        return op switch
        {
            TokenType.Power => 5,
            TokenType.Multiply or TokenType.Divide or TokenType.Modulo => 4,
            TokenType.Plus or TokenType.Minus => 3,
            TokenType.Less or TokenType.Greater or TokenType.LessOrEqual or TokenType.GreaterOrEqual => 2,
            TokenType.Equal or TokenType.NotEqual => 1,
            TokenType.And => 0,
            TokenType.Or => -1,
            TokenType.Question => 0,
            _ => 0
        };
    }

    private BinaryOperator MapBinaryOperator(TokenType type)
    {
        return type switch
        {
            TokenType.Plus => BinaryOperator.Add,
            TokenType.Minus => BinaryOperator.Subtract,
            TokenType.Multiply => BinaryOperator.Multiply,
            TokenType.Divide => BinaryOperator.Divide,
            TokenType.Modulo => BinaryOperator.Modulo,
            TokenType.Power => BinaryOperator.Power,
            TokenType.Equal => BinaryOperator.Equal,
            TokenType.NotEqual => BinaryOperator.NotEqual,
            TokenType.Less => BinaryOperator.LessThan,
            TokenType.Greater => BinaryOperator.GreaterThan,
            TokenType.LessOrEqual => BinaryOperator.LessOrEqual,
            TokenType.GreaterOrEqual => BinaryOperator.GreaterOrEqual,
            TokenType.And => BinaryOperator.And,
            TokenType.Or => BinaryOperator.Or,
            _ => throw new Exception($"Cannot map {type} to BinaryOperator")
        };
    }

    private UnaryOperator MapUnaryOperator(TokenType type)
    {
        return type switch
        {
            TokenType.Plus => UnaryOperator.Plus,
            TokenType.Minus => UnaryOperator.Minus,
            TokenType.Not => UnaryOperator.Not,
            _ => throw new Exception($"Cannot map {type} to UnaryOperator")
        };
    }

    #endregion
}