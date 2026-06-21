// AstroEditor/Core/Data/BindingRouter.cs
// Маршрутизатор значений для реактивных привязок (<=>).
// Находит целевую переменную в глобальных таблицах и обновляет её значение.

using AstroEditor.Core.Binding;
using AstroEditor.Core.Tables;

namespace AstroEditor.Core.Data;

/// <summary>
/// Маршрутизатор значений для привязок.
/// Находит целевую переменную по имени и обновляет её.
/// </summary>
public class BindingRouter
{
    private readonly VariableTableSet _globalTables;
    
    public BindingRouter(VariableTableSet globalTables)
    {
        _globalTables = globalTables ?? throw new ArgumentNullException(nameof(globalTables));
    }
    
    /// <summary>
    /// Пытается найти переменную по имени и установить значение.
    /// </summary>
    public bool RouteValue(string sourceName, object? value, ReactiveBinding binding)
    {
        // Поиск по глобальным таблицам
        var target = _globalTables.FindVariable(binding.TargetName);
        if (target != null)
        {
            target.Value = value;
            return true;
        }

        // Если цель не найдена — привязка «висит в воздухе»
        return false;
    }
}
