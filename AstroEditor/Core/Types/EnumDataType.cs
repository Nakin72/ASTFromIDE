using AstroEditor.Core.Common;

namespace AstroEditor.Core.Types;

/// <summary>
/// Перечисляемый тип данных.
/// Базовый тип — int (для switch/case/присваиваний).
/// </summary>
public class EnumDataType : DataType
{
    public override DataTypeKind Kind => DataTypeKind.Enum;

    /// <summary>Допустимые значения перечисления (имя → целочисленное значение).</summary>
    public Dictionary<string, long> Values { get; set; } = new();

    /// <summary>Автоинкремент для значений по умолчанию.</summary>
    public bool AutoValues { get; set; } = true;

    public EnumDataType()
    {
        Category = DataTypeCategory.User;
    }

    /// <summary>Получить имя по значению.</summary>
    public string? GetName(long value)
    {
        foreach (var kv in Values)
            if (kv.Value == value) return kv.Key;
        return null;
    }

    /// <summary>Получить значение по имени.</summary>
    public long? GetValue(string name)
    {
        return Values.TryGetValue(name, out var v) ? v : null;
    }

    /// <summary>Добавить имя со значением (автоинкремент если AutoValues).</summary>
    public void AddValue(string name, long? explicitValue = null)
    {
        if (Values.ContainsKey(name)) return;
        var value = explicitValue ?? (Values.Count > 0 ? Values.Values.Max() + 1 : 0);
        Values[name] = value;
    }
}
