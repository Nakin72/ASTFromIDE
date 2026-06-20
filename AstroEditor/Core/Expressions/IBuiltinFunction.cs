// AstroEditor.Core/Expressions/IBuiltinFunction.cs
// Интерфейс для встроенных функций (в т.ч. от плагинов)

namespace AstroEditor.Core.Expressions;

/// <summary>
/// Интерфейс для встроенной функции
/// </summary>
public interface IBuiltinFunction
{
    /// <summary>
    /// Выполнить функцию
    /// </summary>
    object? Execute(params object?[] args);

    /// <summary>
    /// Минимальное количество аргументов
    /// </summary>
    int RequiredArgCount { get; }

    /// <summary>
    /// Поддерживает ли переменное количество аргументов
    /// </summary>
    bool HasVariableArgs { get; }
}
