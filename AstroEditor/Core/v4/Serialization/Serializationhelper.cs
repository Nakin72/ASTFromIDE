using System.Text.Json;
using System.Text.Json.Serialization;

namespace AstroEditor.Core.v4.Serialization;

public static class SerializationHelper
{
    public static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            // При необходимости можно добавить другие конвертеры
        }
    };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, DefaultOptions);
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, DefaultOptions);
}