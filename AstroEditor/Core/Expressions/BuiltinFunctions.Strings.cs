// AstroEditor.Core/Expressions/BuiltinFunctions.Strings.cs
// Строковые встроенные функции

namespace AstroEditor.Core.Expressions;

public static partial class BuiltinFunctions
{
    public static Dictionary<string, Func<object?[], object?>> GetStringFunctions()
    {
        return new Dictionary<string, Func<object?[], object?>>
        {
            { "LEN", args => Convert.ToString(args[0])?.Length ?? 0 },
            { "CONCAT", args => string.Concat(args.Select(a => Convert.ToString(a) ?? "")) },
            { "SUBSTR", args =>
                {
                    var str = Convert.ToString(args[0]) ?? "";
                    var start = Convert.ToInt32(args[1]);
                    var length = args.Length > 2 ? Convert.ToInt32(args[2]) : str.Length - start;
                    return str.Substring(start, length);
                }
            },
            { "UPPER", args => Convert.ToString(args[0])?.ToUpper() ?? "" },
            { "LOWER", args => Convert.ToString(args[0])?.ToLower() ?? "" },
            { "TRIM", args => Convert.ToString(args[0])?.Trim() ?? "" },
        };
    }
}
