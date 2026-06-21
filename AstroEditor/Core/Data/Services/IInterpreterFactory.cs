// AstroEditor.Core/Data/Services/IInterpreterFactory.cs
// Фабрика интерпретаторов — вынесена из ProjectManager (P1-8 SRP)

using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Plugins;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Фабрика для создания интерпретаторов.
/// Инкапсулирует логику создания InterpreterContext и ExpressionCache.
/// </summary>
public interface IInterpreterFactory
{
    /// <summary>
    /// Создать интерпретатор для выполнения программы.
    /// </summary>
    AstroInterpreterEx CreateInterpreter();
    
    /// <summary>
    /// Обновить PluginManager (вызывается после InitializeNew).
    /// </summary>
    void UpdatePluginManager(PluginManager? pluginManager);
}
