// AstroEditor.Core/Data/IRuntimeContext.cs
// Интерфейс контекста выполнения для внедрения зависимостей

using AstroEditor.Core.Alarms;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data;

/// <summary>
/// Контекст выполнения - предоставляет доступ ко всем сервисам среды.
/// Используется для внедрения зависимостей и изоляции компонентов.
/// </summary>
public interface IRuntimeContext
{
    // Реестры
    DataTypeRegistry TypeRegistry { get; }
    FormRegistry FormRegistry { get; }
    VariableTableSet GlobalTables { get; }
    IReadOnlyDictionary<string, AstroProgram> Programs { get; }
    Dictionary<string, Func<object?[], object?>> Functions { get; }
    
    // Сервисы
    IAlarmService Alarms { get; }
    IInterruptService Interrupts { get; }
    ITimerService Timers { get; }
    ITaskScheduler Scheduler { get; }
    IBindingService Bindings { get; }
    IExpressionCache ExpressionCache { get; }
    
    // Плагины
    PluginManager? Plugins { get; }
    
    // Логирование
    Microsoft.Extensions.Logging.ILogger? Logger { get; }
}

/// <summary>
/// Базовая реализация контекста выполнения.
/// </summary>
public class RuntimeContext : IRuntimeContext
{
    private readonly ProjectManager _projectManager;
    private readonly IBindingService _bindings;
    private readonly IExpressionCache _expressionCache;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;
    
    public RuntimeContext(
        ProjectManager projectManager,
        IBindingService bindings,
        IExpressionCache expressionCache,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _projectManager = projectManager;
        _bindings = bindings;
        _expressionCache = expressionCache;
        _logger = logger;
    }
    
    // Реестры
    public DataTypeRegistry TypeRegistry => _projectManager.TypeRegistry;
    public FormRegistry FormRegistry => _projectManager.FormRegistry;
    public VariableTableSet GlobalTables => _projectManager.GlobalTables;
    public IReadOnlyDictionary<string, AstroProgram> Programs => _projectManager.Programs;
    public Dictionary<string, Func<object?[], object?>> Functions => _projectManager.Functions;
    
    // Сервисы - делегируем к ProjectManager
    public IAlarmService Alarms => _projectManager.Alarms;
    public IInterruptService Interrupts => _projectManager.Interrupts;
    public ITimerService Timers => _projectManager.Timers;
    public ITaskScheduler Scheduler => _projectManager.CreateScheduler();
    public IBindingService Bindings => _bindings;
    public IExpressionCache ExpressionCache => _expressionCache;
    
    // Плагины
    public PluginManager? Plugins => _projectManager.Plugins;
    
    // Логирование
    public Microsoft.Extensions.Logging.ILogger? Logger => _logger;
}

/// <summary>
/// Контекст для интерпретатора - облегчённая версия.
/// </summary>
public class InterpreterContextEx : IRuntimeContext
{
    public required DataTypeRegistry TypeRegistry { get; init; }
    public required FormRegistry FormRegistry { get; init; }
    public required VariableTableSet GlobalTables { get; init; }
    public required IReadOnlyDictionary<string, AstroProgram> Programs { get; init; }
    public required Dictionary<string, Func<object?[], object?>> Functions { get; init; }
    
    public required IAlarmService Alarms { get; init; }
    public required IInterruptService Interrupts { get; init; }
    public ITimerService? Timers { get; init; }
    public ITaskScheduler? Scheduler { get; init; }
    public IBindingService? Bindings { get; init; }
    public IExpressionCache? ExpressionCache { get; init; }
    
    public PluginManager? Plugins { get; init; }
    public Microsoft.Extensions.Logging.ILogger? Logger { get; init; }
}
