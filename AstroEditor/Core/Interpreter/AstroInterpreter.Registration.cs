// AstroEditor.Core/Interpreter/AstroInterpreter.Registration.cs
// Регистрация внешних обработчиков инструкций
// Интеграция с PluginManager

using AstroEditor.Core.Programs;
using AstroEditor.Core.Plugins;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    /// <summary>
    /// Метод для регистрации внешних обработчиков.
    /// Переопределите в partial классе для добавления своих инструкций.
    /// </summary>
    partial void RegisterExternalHandlers(Dictionary<string, Action<Instruction>> handlers)
    {
        // Если есть PluginManager, регистрируем плагины
        if (_pluginManager != null)
        {
            // Плагины уже зарегистрированы через PluginManager
            // Здесь можно добавить дополнительную логику
        }
        
        // Пустая реализация по умолчанию
        // Для добавления внешних обработчиков создайте другой partial файл
        // и переопределите этот метод
    }
}
