// AstroEditor.Core/Data/Services/InterpreterHostingService.cs
// Сервис создания интерпретаторов и планировщиков

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Types;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Реализация сервиса создания интерпретаторов и планировщиков.
/// </summary>
public class InterpreterHostingService : IInterpreterHostingService
{
    private readonly ILogger _logger;
    private readonly IInterpreterFactory _interpreterFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private Plugins.PluginManager? _pluginManager;

    public InterpreterHostingService(
        IInterpreterFactory interpreterFactory,
        ISchedulerFactory schedulerFactory,
        ILogger? logger = null)
    {
        _interpreterFactory = interpreterFactory ?? throw new ArgumentNullException(nameof(interpreterFactory));
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _logger = logger ?? Log.For<InterpreterHostingService>();
    }

    public void UpdatePluginManager(Plugins.PluginManager? pluginManager)
    {
        try
        {
            _pluginManager = pluginManager;
            _interpreterFactory.UpdatePluginManager(pluginManager);
            _logger.LogDebug("PluginManager updated in InterpreterHostingService");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update PluginManager");
            throw new InterpreterServiceException("Failed to update PluginManager", ex);
        }
    }

    public AstroInterpreterEx CreateInterpreter()
    {
        try
        {
            _logger.LogTrace("Creating new interpreter");
            var interpreter = _interpreterFactory.CreateInterpreter();
            _logger.LogTrace("Interpreter created successfully");
            return interpreter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create interpreter");
            throw new InterpreterServiceException("Failed to create interpreter", ex);
        }
    }

    public Execution.TaskScheduler CreateScheduler(
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programs,
        DataTypeRegistry typeRegistry,
        FormRegistry formRegistry,
        Dictionary<string, Func<object?[], object?>> functions)
    {
        if (globalTables == null)
            throw new ArgumentNullException(nameof(globalTables));
        if (programs == null)
            throw new ArgumentNullException(nameof(programs));
        if (typeRegistry == null)
            throw new ArgumentNullException(nameof(typeRegistry));
        if (formRegistry == null)
            throw new ArgumentNullException(nameof(formRegistry));
        if (functions == null)
            throw new ArgumentNullException(nameof(functions));

        try
        {
            _logger.LogTrace("Creating new scheduler");
            var scheduler = _schedulerFactory.CreateScheduler(
                globalTables,
                programs,
                typeRegistry,
                formRegistry,
                functions
            );
            _logger.LogTrace("Scheduler created successfully");
            return scheduler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create scheduler");
            throw new InterpreterServiceException("Failed to create scheduler", ex);
        }
    }
}

/// <summary>
/// Исключение сервиса создания интерпретаторов.
/// </summary>
public class InterpreterServiceException : Exception
{
    public InterpreterServiceException(string message) : base(message) { }
    public InterpreterServiceException(string message, Exception inner) : base(message, inner) { }
}
