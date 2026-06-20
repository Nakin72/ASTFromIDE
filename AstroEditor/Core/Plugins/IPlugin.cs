// AstroEditor.Core/Plugins/IPlugin.cs
// Интерфейс плагина для расширения функционала AstroEditor

using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Plugins;

namespace AstroEditor.Core.Plugins;

/// <summary>
/// Интерфейс плагина для расширения функционала AstroEditor
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Имя плагина
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Версия плагина
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Описание плагина
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Вызывается при загрузке плагина
    /// </summary>
    void OnLoad(PluginContext context);

    /// <summary>
    /// Вызывается при выгрузке плагина
    /// </summary>
    void OnUnload();
}

/// <summary>
/// Контекст плагина - предоставляет доступ к API AstroEditor
/// </summary>
public class PluginContext
{
    /// <summary>
    /// Регистрация обработчиков инструкций
    /// </summary>
    public Action<string, Action<Instruction>> RegisterInstruction { get; }

    /// <summary>
    /// Регистрация встроенных функций
    /// </summary>
    public Action<string, IBuiltinFunction> RegisterFunction { get; }

    /// <summary>
    /// Регистрация форм инструкций
    /// </summary>
    public Action<FormDefinition> RegisterForm { get; }

    /// <summary>
    /// Регистрация типов данных
    /// </summary>
    public Action<DataType> RegisterType { get; }

    /// <summary>
    /// Логирование от плагина
    /// </summary>
    public Action<string, string> Log { get; }

    public PluginContext(
        Action<string, Action<Instruction>> registerInstruction,
        Action<string, IBuiltinFunction> registerFunction,
        Action<FormDefinition> registerForm,
        Action<DataType> registerType,
        Action<string, string> log)
    {
        RegisterInstruction = registerInstruction;
        RegisterFunction = registerFunction;
        RegisterForm = registerForm;
        RegisterType = registerType;
        Log = log;
    }
}
