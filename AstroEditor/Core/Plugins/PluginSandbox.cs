// AstroEditor.Core/Plugins/PluginSandbox.cs
// Изолированная среда для выполнения плагинов

using System.Reflection;
using System.Runtime.Loader;
using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Plugins;

/// <summary>
/// Изолированный контекст загрузки для плагинов.
/// Позволяет выгружать плагины без перезапуска приложения.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;
    private readonly ILogger _logger;
    
    public PluginLoadContext(string pluginPath, ILogger? logger = null) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _logger = logger ?? Log.For<PluginLoadContext>();
        
        _logger.LogDebug("PluginLoadContext created for {PluginPath}", pluginPath);
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Пробуем загрузить из пути плагина
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            _logger.LogTrace("Loading assembly {AssemblyName} from {Path}", 
                assemblyName.Name, assemblyPath);
            return LoadFromAssemblyPath(assemblyPath);
        }
        
        // Для системных сборок используем стандартный контекст
        return null;
    }
    
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        
        return base.LoadUnmanagedDll(unmanagedDllName);
    }
    
    /// <summary>
    /// Загрузить плагин из DLL.
    /// </summary>
    public Assembly LoadPlugin()
    {
        _logger.LogInformation("Loading plugin from {Path}", _pluginPath);
        return LoadFromAssemblyPath(_pluginPath);
    }
    
    /// <summary>
    /// Выгрузить контекст (и плагин).
    /// </summary>
    public new void Unload()
    {
        _logger.LogInformation("Unloading plugin context for {Path}", _pluginPath);
        base.Unload();
    }
}

/// <summary>
/// Менеджер изолированных плагинов.
/// </summary>
public class PluginSandbox : IDisposable
{
    private readonly Dictionary<string, PluginLoadContext> _contexts = new();
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly ILogger _logger;
    
    public PluginSandbox(ILogger? logger = null)
    {
        _logger = logger ?? Log.For<PluginSandbox>();
    }
    
    /// <summary>
    /// Загрузить плагин в изолированный контекст.
    /// </summary>
    public IPlugin LoadPlugin(string pluginPath, PluginContext pluginContext)
    {
        if (!File.Exists(pluginPath))
            throw new FileNotFoundException("Plugin DLL not found", pluginPath);
        
        var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        
        // Проверяем, не загружен ли уже
        if (_plugins.ContainsKey(pluginName))
        {
            _logger.LogWarning("Plugin {Name} already loaded, unloading first", pluginName);
            UnloadPlugin(pluginName);
        }
        
        try
        {
            // Создаём изолированный контекст
            var loadContext = new PluginLoadContext(pluginPath, _logger);
            _contexts[pluginName] = loadContext;
            
            // Загружаем сборку
            var assembly = loadContext.LoadPlugin();
            
            // Находим тип плагина
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && 
                                    !t.IsInterface && 
                                    !t.IsAbstract);
            
            if (pluginType == null)
                throw new Exception($"No IPlugin implementation found in {pluginPath}");
            
            // Создаём экземпляр
            var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
            
            // Инициализируем
            plugin.OnLoad(pluginContext);
            
            _plugins[pluginName] = plugin;
            
            _logger.LogInformation("Plugin {Name} loaded successfully in sandbox", pluginName);
            
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin {Path}", pluginPath);
            
            // Очищаем контекст при ошибке
            if (_contexts.TryGetValue(pluginName, out var context))
            {
                context.Unload();
                _contexts.Remove(pluginName);
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// Выгрузить плагин.
    /// </summary>
    public void UnloadPlugin(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var plugin))
        {
            _logger.LogWarning("Plugin {Name} not found for unload", pluginName);
            return;
        }
        
        try
        {
            // Вызываем OnUnload
            plugin.OnUnload();
            
            // Удаляем из словаря
            _plugins.Remove(pluginName);
            
            // Выгружаем контекст
            if (_contexts.TryGetValue(pluginName, out var context))
            {
                context.Unload();
                _contexts.Remove(pluginName);
                
                _logger.LogInformation("Plugin {Name} unloaded successfully", pluginName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin {Name}", pluginName);
        }
    }
    
    /// <summary>
    /// Получить загруженный плагин по имени.
    /// </summary>
    public IPlugin? GetPlugin(string pluginName)
    {
        return _plugins.TryGetValue(pluginName, out var plugin) ? plugin : null;
    }
    
    /// <summary>
    /// Получить все загруженные плагины.
    /// </summary>
    public IReadOnlyDictionary<string, IPlugin> GetLoadedPlugins() => _plugins.AsReadOnly();
    
    /// <summary>
    /// Выгрузить все плагины.
    /// </summary>
    public void UnloadAll()
    {
        _logger.LogInformation("Unloading all plugins ({Count})", _plugins.Count);
        
        foreach (var pluginName in _plugins.Keys.ToList())
        {
            UnloadPlugin(pluginName);
        }
    }
    
    /// <summary>
    /// Статус sandbox.
    /// </summary>
    public PluginSandboxStatus GetStatus()
    {
        return new PluginSandboxStatus
        {
            LoadedPluginsCount = _plugins.Count,
            ActiveContextsCount = _contexts.Count,
            Plugins = _plugins.Keys.ToList()
        };
    }
    
    public void Dispose()
    {
        UnloadAll();
    }
}

/// <summary>
/// Статус PluginSandbox.
/// </summary>
public class PluginSandboxStatus
{
    public int LoadedPluginsCount { get; init; }
    public int ActiveContextsCount { get; init; }
    public List<string> Plugins { get; init; } = new();
    
    public override string ToString() => 
        $"Plugins: {LoadedPluginsCount}, Contexts: {ActiveContextsCount}";
}

