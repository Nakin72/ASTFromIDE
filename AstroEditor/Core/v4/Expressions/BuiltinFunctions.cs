// AstroEditor.Core.v4/Expressions/BuiltinFunctions.cs
namespace AstroEditor.Core.v4.Expressions;

public static class BuiltinFunctions
{
    public static Dictionary<string, Func<object?[], object?>> GetFunctions()
    {
        return new Dictionary<string, Func<object?[], object?>>
        {
            // Математические
            { "SIN", args => Math.Sin(Convert.ToDouble(args[0])) },
            { "COS", args => Math.Cos(Convert.ToDouble(args[0])) },
            { "TAN", args => Math.Tan(Convert.ToDouble(args[0])) },
            { "ASIN", args => Math.Asin(Convert.ToDouble(args[0])) },
            { "ACOS", args => Math.Acos(Convert.ToDouble(args[0])) },
            { "ATAN", args => Math.Atan(Convert.ToDouble(args[0])) },
            { "SQRT", args => Math.Sqrt(Convert.ToDouble(args[0])) },
            { "EXP", args => Math.Exp(Convert.ToDouble(args[0])) },
            { "LOG", args => Math.Log(Convert.ToDouble(args[0])) },
            { "LOG10", args => Math.Log10(Convert.ToDouble(args[0])) },
            { "ABS", args => Math.Abs(Convert.ToDouble(args[0])) },
            { "ROUND", args => Math.Round(Convert.ToDouble(args[0])) },
            { "FLOOR", args => Math.Floor(Convert.ToDouble(args[0])) },
            { "CEIL", args => Math.Ceiling(Convert.ToDouble(args[0])) },
            // Строковые
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