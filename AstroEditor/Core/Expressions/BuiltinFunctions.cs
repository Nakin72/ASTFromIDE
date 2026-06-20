// AstroEditor.Core/Expressions/BuiltinFunctions.cs
// Главный файл для регистрации всех встроенных функций
// Объединяет функции из разных категорий

using System.Text.Json;

namespace AstroEditor.Core.Expressions;

public static partial class BuiltinFunctions
{
    /// <summary>
    /// Получить все встроенные функции
    /// </summary>
    public static Dictionary<string, Func<object?[], object?>> GetFunctions()
    {
        var functions = new Dictionary<string, Func<object?[], object?>>();

        // Математические
        foreach (var kv in GetMathFunctions())
            functions[kv.Key] = kv.Value;

        // Строковые
        foreach (var kv in GetStringFunctions())
            functions[kv.Key] = kv.Value;

        // Массивы
        foreach (var kv in GetArrayFunctions())
            functions[kv.Key] = kv.Value;

        return functions;
    }

    /// <summary>
    /// Нормализует входное значение в список.
    /// Поддерживает List<object>, Array, JsonElement (array).
    /// </summary>
    private static List<object?>? NormalizeArray(object? value)
    {
        if (value == null) return null;
        if (value is List<object?> list) return list;
        if (value is Array arr)
        {
            var result = new List<object?>();
            foreach (var item in arr)
                result.Add(item);
            return result;
        }
        if (value is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            return json.EnumerateArray().Select(e =>
            {
                return e.ValueKind switch
                {
                    JsonValueKind.String => e.GetString(),
                    JsonValueKind.Number => e.TryGetInt32(out var i) ? (object?)i : e.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => e.GetRawText()
                };
            }).ToList();
        }
        // Если это не массив, возвращаем null
        return null;
    }
}
