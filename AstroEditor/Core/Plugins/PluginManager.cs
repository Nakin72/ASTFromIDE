// AstroEditor.Core/Plugins/PluginManager.cs
// Менеджер плагинов - загрузка, регистрация, управление жизненным циклом
// Использует PluginSandbox для изолированной загрузки плагинов

using System.Reflection;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();
    private readonly List<(string FormId, Action<Instruction> Handler)> _instructionHandlers = new();
    private readonly List<(string Name, IBuiltinFunction Function)> _functions = new();
    private readonly ScriptPluginLoader? _scriptLoader;
    private readonly CSharpScriptEngine? _scriptEngine;
    private readonly PluginSandbox _sandbox;
    private readonly ILogger _logger;
    
    // ✅ P0-4: Lock для потокобезопасного доступа к коллекциям
    private readonly object _lock = new();
    
    private readonly Action<string, Action<Instruction>> _registerInstruction;
    private readonly Action<string, IBuiltinFunction> _registerFunction;
    private readonly Action<FormDefinition> _registerForm;
    private readonly Action<DataType> _registerType;

    public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();
    public string PluginsFolder { get; }
    public ScriptPluginLoader? ScriptLoader => _scriptLoader;
    public CSharpScriptEngine? ScriptEngine => _scriptEngine;
    public PluginSandboxStatus SandboxStatus => _sandbox.GetStatus();

    public PluginManager(
        string pluginsFolder,
        Action<string, Action<Instruction>> registerInstruction,
        Action<string, IBuiltinFunction> registerFunction,
        Action<FormDefinition> registerForm,
        Action<DataType> registerType,
        bool enableScripting = true,
        ILogger? logger = null)
    {
        PluginsFolder = pluginsFolder;
        _registerInstruction = registerInstruction;
        _registerFunction = registerFunction;
        _registerForm = registerForm;
        _registerType = registerType;
        _logger = logger ?? Log.For<PluginManager>();
        _sandbox = new PluginSandbox(_logger);

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
        
        _logger.LogInformation("PluginManager initialized, folder: {Folder}, Scripting: {Enabled}", 
            pluginsFolder, enableScripting ? "enabled" : "disabled");
    }

    /// <summary>
    /// Загрузить все плагины из папки
    /// </summary>
    public void LoadAllPlugins()
    {
        _logger.LogInformation("Loading all plugins from {Folder}", PluginsFolder);
        
        if (!Directory.Exists(PluginsFolder))
        {
            _logger.LogDebug("Plugins folder does not exist, creating: {Folder}", PluginsFolder);
            Directory.CreateDirectory(PluginsFolder);
            return;
        }

        var loadedCount = 0;
        var errorCount = 0;
        
        // 1. Загружаем скомпилированные .dll через PluginSandbox
        var dllFiles = Directory.GetFiles(PluginsFolder, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var dllFile in dllFiles)
        {
            try
            {
                LoadPluginSandboxed(dllFile);
                loadedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "ERROR loading {PluginFile}", Path.GetFileName(dllFile));
            }
        }

        // 2. Загружаем и компилируем скрипты .cs
        _scriptLoader?.LoadAllScripts();
        
        _logger.LogInformation("Plugins loaded: {Loaded} successful, {Errors} errors", loadedCount, errorCount);
    }

    /// <summary>
    /// Загрузить плагин через изолированный контекст (PluginSandbox).
    /// </summary>
    private void LoadPluginSandboxed(string dllPath)
    {
        var context = new PluginContext(
            _registerInstruction,
            _registerFunction,
            _registerForm,
            _registerType,
            (level, msg) => _logger.LogInformation("[{Level}] {Message}", level, msg)
        );
        
        var plugin = _sandbox.LoadPlugin(dllPath, context);
        
        // Получаем атрибут плагина для логирования
        var pluginType = plugin.GetType();
        var attr = pluginType.GetCustomAttribute<PluginAttribute>();
        
        lock (_lock) // ✅ P0-4: блокировка
        {
            if (attr != null)
            {
                _loadedPlugins[attr.Name] = plugin;
                _logger.LogInformation("LOADED: {Name} v{Version} - {Description}", 
                    attr.Name, attr.Version, attr.Description);
            }
            else
            {
                var pluginName = Path.GetFileNameWithoutExtension(dllPath);
                _loadedPlugins[pluginName] = plugin;
                _logger.LogInformation("LOADED: {Name} (no attribute)", pluginName);
            }
        }
    }

    /// <summary>
    /// Выгрузить плагин по имени.
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        IPlugin? pluginToUnload = null;
        lock (_lock)
        {
            _loadedPlugins.TryGetValue(pluginName, out pluginToUnload);
        }
        
        if (pluginToUnload != null)
        {
            try
            {
                pluginToUnload.OnUnload();
                lock (_lock) // ✅ P0-4: блокировка
                {
                    _loadedPlugins.Remove(pluginName);
                }
                _sandbox.UnloadPlugin(pluginName);
                _logger.LogInformation("UNLOADED: {Name}", pluginName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR unloading {Name}", pluginName);
            }
        }
        else
        {
            _logger.LogWarning("Plugin {Name} not found for unload", pluginName);
        }
    }

    /// <summary>
    /// Выгрузить все плагины.
    /// </summary>
    public void UnloadAll()
    {
        _logger.LogInformation("Unloading all plugins ({Count})", _loadedPlugins.Count);
        
        List<IPlugin> pluginsToUnload;
        lock (_lock)
        {
            pluginsToUnload = _loadedPlugins.Values.ToList();
            _loadedPlugins.Clear();
        }
        
        foreach (var plugin in pluginsToUnload)
        {
            try
            {
                plugin.OnUnload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR unloading plugin");
            }
        }
        
        _sandbox.UnloadAll();
        
        _logger.LogInformation("All plugins unloaded");
    }

    /// <summary>
    /// Зарегистрировать обработчик инструкции от плагина
    /// </summary>
    public void RegisterPluginInstruction(string formId, Action<Instruction> handler)
    {
        lock (_lock) // ✅ P0-4: блокировка
        {
            _instructionHandlers.Add((formId, handler));
        }
        _registerInstruction(formId, handler);
        _logger.LogDebug("Registered plugin instruction handler: {FormId}", formId);
    }

    /// <summary>
    /// Зарегистрировать функцию от плагина
    /// </summary>
    public void RegisterPluginFunction(string name, IBuiltinFunction function)
    {
        lock (_lock) // ✅ P0-4: блокировка
        {
            _functions.Add((name, function));
        }
        _registerFunction(name, function);
        _logger.LogDebug("Registered plugin function: {Name}", name);
    }
    
    /// <summary>
    /// Получить статус менеджера плагинов.
    /// </summary>
    public PluginManagerStatus GetStatus()
    {
        var sandboxStatus = _sandbox.GetStatus();
        return new PluginManagerStatus
        {
            LoadedPluginsCount = _loadedPlugins.Count,
            InstructionHandlersCount = _instructionHandlers.Count,
            FunctionsCount = _functions.Count,
            SandboxLoadedCount = sandboxStatus.LoadedPluginsCount,
            SandboxActiveContexts = sandboxStatus.ActiveContextsCount
        };
    }
}

/// <summary>
/// Статус менеджера плагинов.
/// </summary>
public class PluginManagerStatus
{
    public int LoadedPluginsCount { get; init; }
    public int InstructionHandlersCount { get; init; }
    public int FunctionsCount { get; init; }
    public int SandboxLoadedCount { get; init; }
    public int SandboxActiveContexts { get; init; }
    
    public override string ToString() => 
        $"Plugins: {LoadedPluginsCount}, Handlers: {InstructionHandlersCount}, Functions: {FunctionsCount}, " +
        $"Sandbox: {SandboxLoadedCount} loaded, {SandboxActiveContexts} contexts";
}
