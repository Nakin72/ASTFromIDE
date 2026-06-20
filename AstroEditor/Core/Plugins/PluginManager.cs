// AstroEditor.Core/Plugins/PluginManager.cs
// Менеджер плагинов - загрузка, регистрация, управление жизненным циклом

using System.Reflection;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
    private readonly List<(string FormId, Action<Instruction> Handler)> _instructionHandlers = new();
    private readonly List<(string Name, IBuiltinFunction Function)> _functions = new();
    private readonly ScriptPluginLoader? _scriptLoader;
    private readonly CSharpScriptEngine? _scriptEngine;
    
    private readonly Action<string, Action<Instruction>> _registerInstruction;
    private readonly Action<string, IBuiltinFunction> _registerFunction;
    private readonly Action<FormDefinition> _registerForm;
    private readonly Action<DataType> _registerType;

    public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();
    public string PluginsFolder { get; }
    public ScriptPluginLoader? ScriptLoader => _scriptLoader;
    public CSharpScriptEngine? ScriptEngine => _scriptEngine;

    public PluginManager(
        string pluginsFolder,
        Action<string, Action<Instruction>> registerInstruction,
        Action<string, IBuiltinFunction> registerFunction,
        Action<FormDefinition> registerForm,
        Action<DataType> registerType,
        bool enableScripting = true)
    {
        PluginsFolder = pluginsFolder;
        _registerInstruction = registerInstruction;
        _registerFunction = registerFunction;
        _registerForm = registerForm;
        _registerType = registerType;

        if (enableScripting)
        {
            var scriptsFolder = Path.Combine(pluginsFolder, "Scripts");
            var cacheFolder = Path.Combine(pluginsFolder, "Cache");
            _scriptLoader = new ScriptPluginLoader(
                scriptsFolder,
                cacheFolder,
                this,
                registerInstruction,
                registerFunction,
                registerForm,
                registerType
            );
            _scriptEngine = new CSharpScriptEngine();
        }
    }

    /// <summary>
    /// Загрузить все плагины из папки
    /// </summary>
    public void LoadAllPlugins()
    {
        if (!Directory.Exists(PluginsFolder))
        {
            Directory.CreateDirectory(PluginsFolder);
            return;
        }

        // 1. Загружаем скомпилированные .dll
        var dllFiles = Directory.GetFiles(PluginsFolder, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var dllFile in dllFiles)
        {
            try
            {
                LoadPlugin(dllFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] ERROR loading {Path.GetFileName(dllFile)}: {ex.Message}");
            }
        }

        // 2. Загружаем и компилируем скрипты .cs
        _scriptLoader?.LoadAllScripts();
    }

    /// <summary>
    /// Загрузить плагин из файла
    /// </summary>
    public void LoadPlugin(string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);
        
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            var attr = pluginType.GetCustomAttribute<PluginAttribute>();
            if (attr == null)
            {
                Console.WriteLine($"[PluginManager] WARNING: {pluginType.Name} has no [Plugin] attribute");
                continue;
            }

            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                var context = new PluginContext(
                    _registerInstruction,
                    _registerFunction,
                    _registerForm,
                    _registerType,
                    (level, msg) => Console.WriteLine($"[{attr.Name}] {level}: {msg}")
                );

                plugin.OnLoad(context);
                _loadedPlugins[attr.Name] = plugin;

                Console.WriteLine($"[PluginManager] LOADED: {attr.Name} v{attr.Version} - {attr.Description}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] ERROR initializing {attr.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Выгрузить плагин по имени
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var plugin))
        {
            try
            {
                plugin.OnUnload();
                _loadedPlugins.Remove(pluginName);
                Console.WriteLine($"[PluginManager] UNLOADED: {pluginName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] ERROR unloading {pluginName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Выгрузить все плагины
    /// </summary>
    public void UnloadAll()
    {
        foreach (var plugin in _loadedPlugins.Values.ToList())
        {
            try
            {
                plugin.OnUnload();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginManager] ERROR unloading: {ex.Message}");
            }
        }
        _loadedPlugins.Clear();
    }

    /// <summary>
    /// Зарегистрировать обработчик инструкции от плагина
    /// </summary>
    public void RegisterPluginInstruction(string formId, Action<Instruction> handler)
    {
        _instructionHandlers.Add((formId, handler));
        _registerInstruction(formId, handler);
    }

    /// <summary>
    /// Зарегистрировать функцию от плагина
    /// </summary>
    public void RegisterPluginFunction(string name, IBuiltinFunction function)
    {
        _functions.Add((name, function));
        _registerFunction(name, function);
    }
}
