using System.Globalization;
using System.Text;

namespace AstroEditor.Core.Expressions;

public static class Lexer
{
    public static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var pos = 0;
        var length = input.Length;

        while (pos < length)
        {
            var ch = input[pos];

            if (char.IsWhiteSpace(ch)) { pos++; continue; }

            // Числа (целые и с плавающей точкой)
            if (char.IsDigit(ch) || (ch == '.' && pos + 1 < length && char.IsDigit(input[pos + 1])))
            {
                var start = pos;
                var hasDot = false;
                while (pos < length && (char.IsDigit(input[pos]) || input[pos] == '.'))
                {
                    if (input[pos] == '.')
                    {
                        if (hasDot) break;
                        hasDot = true;
                    }
                    pos++;
                }
                var numberText = input.Substring(start, pos - start);
                try
                {
                    if (hasDot)
                    {
                        var value = double.Parse(numberText, CultureInfo.InvariantCulture);
                        tokens.Add(new Token(TokenType.Number, numberText, start, value));
                    }
                    else
                    {
                        var value = int.Parse(numberText, CultureInfo.InvariantCulture);
                        tokens.Add(new Token(TokenType.Number, numberText, start, value));
                    }
                }
                catch (FormatException)
                {
                    throw new Exception($"Invalid number format '{numberText}' at position {start}");
                }
                continue;
            }

            // Строки в двойных кавычках
            if (ch == '"')
            {
                var start = pos;
                pos++; // пропускаем "
                var sb = new StringBuilder();
                while (pos < length && input[pos] != '"')
                {
                    if (input[pos] == '\\' && pos + 1 < length)
                    {
                        var next = input[pos + 1];
                        if (next == 'n') sb.Append('\n');
                        else if (next == 't') sb.Append('\t');
                        else if (next == '"') sb.Append('"');
                        else sb.Append(next);
                        pos += 2;
                    }
                    else
                    {
                        sb.Append(input[pos]);
                        pos++;
                    }
                }
                if (pos >= length || input[pos] != '"')
                    throw new Exception($"Unterminated string at position {start}");
                pos++; // пропускаем закрывающую "
                tokens.Add(new Token(TokenType.String, input.Substring(start, pos - start), start, sb.ToString()));
                continue;
            }

            // Идентификаторы (буквы, цифры, подчёркивание)
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = pos;
                while (pos < length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
                    pos++;
                var text = input.Substring(start, pos - start);
                if (text == "true") tokens.Add(new Token(TokenType.Bool, text, start, true));
                else if (text == "false") tokens.Add(new Token(TokenType.Bool, text, start, false));
                else tokens.Add(new Token(TokenType.Identifier, text, start));
                continue;
            }

            // Операторы и другие символы
            switch (ch)
            {
                case '+': tokens.Add(new Token(TokenType.Plus, "+", pos)); pos++; break;
                case '-': tokens.Add(new Token(TokenType.Minus, "-", pos)); pos++; break;
                case '*': tokens.Add(new Token(TokenType.Multiply, "*", pos)); pos++; break;
                case '/': tokens.Add(new Token(TokenType.Divide, "/", pos)); pos++; break;
                case '%': tokens.Add(new Token(TokenType.Modulo, "%", pos)); pos++; break;
                case '^': tokens.Add(new Token(TokenType.Power, "^", pos)); pos++; break;
                case '(': tokens.Add(new Token(TokenType.LeftParen, "(", pos)); pos++; break;
                case ')': tokens.Add(new Token(TokenType.RightParen, ")", pos)); pos++; break;
                case '[': tokens.Add(new Token(TokenType.LeftBracket, "[", pos)); pos++; break;
                case ']': tokens.Add(new Token(TokenType.RightBracket, "]", pos)); pos++; break;
                case '.': tokens.Add(new Token(TokenType.Dot, ".", pos)); pos++; break;
                case ',': tokens.Add(new Token(TokenType.Comma, ",", pos)); pos++; break;
                case '?': tokens.Add(new Token(TokenType.Question, "?", pos)); pos++; break;
                case ':': tokens.Add(new Token(TokenType.Colon, ":", pos)); pos++; break;
                case '!':
                    if (pos + 1 < length && input[pos + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.NotEqual, "!=", pos));
                        pos += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Not, "!", pos));
                        pos++;
                    }
                    break;
                case '=':
                    if (pos + 1 < length && input[pos + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.Equal, "==", pos));
                        pos += 2;
                    }
                    else throw new Exception($"Unexpected '=' at position {pos}");
                    break;
                case '<':
                    if (pos + 1 < length && input[pos + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.LessOrEqual, "<=", pos));
                        pos += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Less, "<", pos));
                        pos++;
                    }
                    break;
                case '>':
                    if (pos + 1 < length && input[pos + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.GreaterOrEqual, ">=", pos));
                        pos += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Greater, ">", pos));
                        pos++;
                    }
                    break;
                case '&':
                    if (pos + 1 < length && input[pos + 1] == '&')
                    {
                        tokens.Add(new Token(TokenType.And, "&&", pos));
                        pos += 2;
                    }
                    else throw new Exception($"Unexpected '&' at position {pos}");
                    break;
                case '|':
                    if (pos + 1 < length && input[pos + 1] == '|')
                    {
                        tokens.Add(new Token(TokenType.Or, "||", pos));
                        pos += 2;
                    }
                    else throw new Exception($"Unexpected '|' at position {pos}");
                    break;
                default:
                    throw new Exception($"Unexpected character '{ch}' at position {pos}");
            }
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", pos));
        return tokens;
    }
}