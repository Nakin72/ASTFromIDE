// AstroEditor/Core/Binding/ReactiveBinding.cs
// Оператор <=> — реактивная привязка переменных
// alias <=> target — alias становится "прозрачным", любое чтение/запись идёт через target
// ✅ P0-1: Удалён статический BindingManager — использовать только ThreadSafeBindingManager

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

