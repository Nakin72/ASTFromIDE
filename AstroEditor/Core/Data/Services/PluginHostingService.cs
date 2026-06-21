// AstroEditor.Core/Data/Services/PluginHostingService.cs
// Сервис управления плагинами

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Реализация сервиса управления плагинами.
/// </summary>
public class PluginHostingService : IPluginHostingService
{
    private readonly ILogger _logger;
    private readonly ProjectState _state;
    private string? _projectFolder;

    public PluginManager? PluginManager { get; private set; }

    public PluginHostingService(ProjectState state, ILogger? logger = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _logger = logger ?? Log.For<PluginHostingService>();
    }

    public void Initialize(string projectFolder, ITypeService typeService)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
            throw new ArgumentException("Project folder cannot be empty", nameof(projectFolder));
        
        if (typeService == null)
            throw new ArgumentNullException(nameof(typeService));

        try
        {
            _projectFolder = projectFolder;
            var pluginsFolder = Path.Combine(projectFolder, "Plugins");
            Directory.CreateDirectory(pluginsFolder);

            PluginManager = new PluginManager(
                pluginsFolder,
                (formId, handler) => { }, // Заглушка — реальная регистрация при создании интерпретатора
                (name, func) => _state.Functions[name] = func.Execute,
                form => typeService.RegisterForm(form),
                type => typeService.RegisterType(type),
                true,
                _logger
            );
            _logger.LogDebug("Plugin hosting initialized: {Folder}", pluginsFolder);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to plugins folder: {Folder}", projectFolder);
            throw new PluginServiceException($"Access denied to plugins folder: {projectFolder}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error initializing plugin hosting: {Folder}", projectFolder);
            throw new PluginServiceException($"IO error initializing plugins: {projectFolder}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing plugin hosting: {Folder}", projectFolder);
            throw new PluginServiceException($"Failed to initialize plugin hosting: {projectFolder}", ex);
        }
    }

    public void LoadAllPlugins()
    {
        if (PluginManager == null)
        {
            _logger.LogWarning("Plugin manager not initialized - skipping plugin load");
            return;
        }

        try
        {
            _logger.LogInformation("Loading all plugins");
            PluginManager.LoadAllPlugins();
            _logger.LogInformation("Plugins loaded successfully");
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Plugin file not found");
            // Не выбрасываем — отсутствие плагинов не критично
        }
        catch (BadImageFormatException ex)
        {
            _logger.LogError(ex, "Invalid plugin assembly format");
            throw new PluginServiceException("Invalid plugin assembly format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading plugins");
            throw new PluginServiceException("Failed to load plugins", ex);
        }
    }

    public void UnloadAllPlugins()
    {
        if (PluginManager == null)
        {
            _logger.LogWarning("Plugin manager not initialized - nothing to unload");
            return;
        }

        try
        {
            _logger.LogInformation("Unloading all plugins");
            PluginManager.UnloadAll();
            _logger.LogInformation("Plugins unloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error unloading plugins");
            throw new PluginServiceException("Failed to unload plugins", ex);
        }
    }
}

/// <summary>
/// Исключение сервиса управления плагинами.
/// </summary>
public class PluginServiceException : Exception
{
    public PluginServiceException(string message) : base(message) { }
    public PluginServiceException(string message, Exception inner) : base(message, inner) { }
}

