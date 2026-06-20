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
public static class BindingRouter
{
    /// <summary>
    /// Пытается найти переменную по имени и установить значение.
    /// </summary>
    public static bool RouteValue(string sourceName, object? value, ReactiveBinding binding)
    {
        // Поиск по глобальным таблицам через ProjectManager
        var global = ProjectManager.Instance?.GlobalTables;
        if (global != null)
        {
            var target = global.FindVariable(binding.TargetName);
            if (target != null)
            {
                target.Value = value;
                return true;
            }
        }

        // Если цель не найдена — привязка «висит в воздухе»
        return false;
    }
}
