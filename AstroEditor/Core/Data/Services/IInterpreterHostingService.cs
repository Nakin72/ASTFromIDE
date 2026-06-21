// AstroEditor.Core/Data/Services/IInterpreterHostingService.cs
// Интерфейс сервиса создания интерпретаторов и планировщиков

using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Types;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис создания интерпретаторов и планировщиков задач.
/// </summary>
public interface IInterpreterHostingService
{
    /// <summary>
    /// Обновить PluginManager (вызывается после инициализации плагинов).
    /// </summary>
    void UpdatePluginManager(Plugins.PluginManager? pluginManager);
    
    /// <summary>
    /// Создать новый интерпретатор для выполнения программы.
    /// </summary>
    AstroInterpreterEx CreateInterpreter();

    /// <summary>
    /// Создать новый планировщик задач для многозадачного выполнения.
    /// </summary>
    Execution.TaskScheduler CreateScheduler(
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programs,
        DataTypeRegistry typeRegistry,
        FormRegistry formRegistry,
        Dictionary<string, Func<object?[], object?>> functions);
}
