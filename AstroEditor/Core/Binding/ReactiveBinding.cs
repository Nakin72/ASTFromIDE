// AstroEditor/Core/Binding/ReactiveBinding.cs
// Оператор <=> — реактивная привязка переменных
// alias <=> target — alias становится "прозрачным", любое чтение/запись идёт через target

namespace AstroEditor.Core.Binding;

/// <summary>
/// Тип привязки между переменными.
/// </summary>
public enum BindingDirection
{
    /// <summary>alias <=> target — двусторонняя синхронизация</summary>
    Bidirectional,
    /// <summary>alias => target — alias пишет в target, но не читает из него</summary>
    OneWayToTarget,
    /// <summary>alias <= target — alias читает из target, но не пишет в него</summary>
    OneWayFromTarget
}

/// <summary>
/// Представляет реактивную привязку между двумя переменными.
/// Хранит имя источника и цели, а также направление.
/// </summary>
public class ReactiveBinding
{
    /// <summary>Локальное имя alias</summary>
    public string AliasName { get; init; } = string.Empty;
    
    /// <summary>Целевое имя (глобальная переменная, регистр, DO[n])</summary>
    public string TargetName { get; init; } = string.Empty;
    
    /// <summary>Направление привязки</summary>
    public BindingDirection Direction { get; init; } = BindingDirection.Bidirectional;

    /// <summary>Признак: привязка активна</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Глобальный менеджер привязок. Хранит все <=> связи и управляет их жизненным циклом.
/// </summary>
public static class BindingManager
{
    private static readonly Dictionary<string, List<ReactiveBinding>> _bindings = new();

    /// <summary>
    /// Создаёт реактивную привязку aliasName <=> targetName
    /// </summary>
    public static ReactiveBinding Bind(string aliasName, string targetName, BindingDirection direction = BindingDirection.Bidirectional)
    {
        var binding = new ReactiveBinding
        {
            AliasName = aliasName,
            TargetName = targetName,
            Direction = direction
        };

        if (!_bindings.ContainsKey(aliasName))
            _bindings[aliasName] = new List<ReactiveBinding>();

        _bindings[aliasName].Add(binding);
        return binding;
    }

    /// <summary>
    /// Удаляет все привязки для указанного alias.
    /// </summary>
    public static void Unbind(string aliasName)
    {
        _bindings.Remove(aliasName);
    }

    /// <summary>
    /// Удаляет конкретную привязку.
    /// </summary>
    public static void Unbind(ReactiveBinding binding)
    {
        if (_bindings.TryGetValue(binding.AliasName, out var list))
        {
            list.Remove(binding);
            if (list.Count == 0)
                _bindings.Remove(binding.AliasName);
        }
    }

    /// <summary>
    /// Возвращает все привязки для alias.
    /// </summary>
    public static IReadOnlyList<ReactiveBinding> GetBindings(string aliasName)
    {
        return _bindings.TryGetValue(aliasName, out var list)
            ? list.AsReadOnly()
            : Array.Empty<ReactiveBinding>();
    }

    /// <summary>
    /// Очищает все привязки (при смене проекта).
    /// </summary>
    public static void Clear()
    {
        _bindings.Clear();
    }

    /// <summary>
    /// Проверяет, есть ли активная привязка у alias.
    /// </summary>
    public static bool IsBound(string aliasName)
    {
        return _bindings.TryGetValue(aliasName, out var list) && list.Any(b => b.IsActive);
    }
}