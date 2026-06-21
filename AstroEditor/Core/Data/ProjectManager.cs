// AstroEditor/Core/Data/ProjectManager.cs
// Центральный менеджер проекта.
// Координирует сервисы жизненного цикла, плагинов и интерпретаторов (SRP).
// ✅ P3: Разделение ответственности через специализированные сервисы

using AstroEditor.Core.Binding;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Common;
using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Serialization;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Data.Services;
using Microsoft.Extensions.Logging;
using AstroGlobalTables = AstroEditor.Core.Tables.VariableTableSet;
using AstroScheduler = AstroEditor.Core.Execution.TaskScheduler;

namespace AstroEditor.Core.Data;

/// <summary>
/// Состояние проекта (сохраняемая часть).
/// </summary>
public class ProjectState
{
    public DataTypeRegistry TypeRegistry { get; set; } = new();
    public FormRegistry FormRegistry { get; set; } = new();
    public AstroGlobalTables GlobalTables { get; set; } = new() { Name = "GlobalVariables", IsGlobal = true };
    public Dictionary<string, Programs.AstroProgram> Programs { get; set; } = new();
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; } = new();
}

/// <summary>
/// Менеджер проекта — координирует сервисы.
/// ✅ P3: Делегирует ответственность специализированным сервисам:
///   - IProjectLifecycleService: инициализация, сохранение, загрузка
///   - IPluginHostingService: управление плагинами
///   - IInterpreterHostingService: создание интерпретаторов и планировщиков
///   - ITypeService: управление типами и формами
///   - IRuntimeService: аварии, прерывания, таймеры
///   - IBindingService: привязки переменных
/// </summary>
public class ProjectManager
{
    private readonly ProjectState _state;
    private readonly ILogger _logger;
    
    // Сервисы
    private readonly IProjectLifecycleService _lifecycleService;
    private readonly IPluginHostingService _pluginHostingService;
    private readonly IInterpreterHostingService _interpreterHostingService;
    private readonly ITypeService _typeService;
    private readonly IRuntimeService _runtimeService;
    private readonly IBindingService _bindingService;
    private readonly IProgramService _programService;
    
    // Публичный доступ к сервисам
    public PluginManager? Plugins => _pluginHostingService.PluginManager;
    public string ProjectFolder => _lifecycleService.ProjectFolder;
    public bool HasUnsavedChanges => _lifecycleService.HasUnsavedChanges;
    public DataTypeRegistry TypeRegistry => _typeService.TypeRegistry;
    public FormRegistry FormRegistry => _typeService.FormRegistry;
    public AstroGlobalTables GlobalTables => _state.GlobalTables;
    public IReadOnlyDictionary<string, Programs.AstroProgram> Programs => _programService.Programs;
    public Dictionary<string, Func<object?[], object?>> Functions => _state.Functions;
    
    // Runtime-сервисы
    public IAlarmService Alarms => _runtimeService.Alarms;
    public IInterruptService Interrupts => _runtimeService.Interrupts;
    public ITimerService Timers => _runtimeService.Timers;

    // Сервис привязок
    public IBindingService Bindings => _bindingService;
    
    // Событие изменения проекта
    public event Action? OnProjectChanged;

    /// <summary>
    /// Конструктор для DI с полным набором сервисов.
    /// </summary>
    public ProjectManager(
        ProjectState state,
        IProjectLifecycleService lifecycleService,
        IPluginHostingService pluginHostingService,
        IInterpreterHostingService interpreterHostingService,
        ITypeService typeService,
        IRuntimeService runtimeService,
        IBindingService bindingService,
        IProgramService programService,
        ILogger<ProjectManager>? logger = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _pluginHostingService = pluginHostingService ?? throw new ArgumentNullException(nameof(pluginHostingService));
        _interpreterHostingService = interpreterHostingService ?? throw new ArgumentNullException(nameof(interpreterHostingService));
        _typeService = typeService ?? throw new ArgumentNullException(nameof(typeService));
        _runtimeService = runtimeService ?? throw new ArgumentNullException(nameof(runtimeService));
        _bindingService = bindingService ?? throw new ArgumentNullException(nameof(bindingService));
        _programService = programService ?? throw new ArgumentNullException(nameof(programService));
        _logger = logger ?? Log.For<ProjectManager>();
        
        // Подписка на событие изменения проекта
        _lifecycleService.OnProjectChanged += () => OnProjectChanged?.Invoke();
        
        _logger.LogDebug("ProjectManager created with DI services");
    }
        
    /// <summary>
    /// Конструктор по умолчанию (для обратной совместимости).
    /// Создаёт все сервисы внутри себя.
    /// </summary>
    public ProjectManager()
    {
        // ✅ P0-1: Единый экземпляр ProjectState
        _state = new ProjectState();
        _logger = Log.For<ProjectManager>();
        
        // Создаём базовые сервисы
        _bindingService = new ThreadSafeBindingManager(_state.GlobalTables, _logger);
        _programService = new ProgramService(_state, _logger);
        _typeService = new TypeService(_state, _logger);
        _runtimeService = new RuntimeService(_state, null, _logger);
        
        // Создаём хранилище
        var storage = new ProjectStorageService(_state, _logger);
        
        // Создаём сервис жизненного цикла
        _lifecycleService = new ProjectLifecycleService(_state, storage, _programService, _logger);
        
        // Создаём сервис плагинов
        _pluginHostingService = new PluginHostingService(_state, _logger);
        
        // Создаём фабрики
        var interpreterFactory = new InterpreterFactory(
            _state.TypeRegistry,
            _state.FormRegistry,
            _state.GlobalTables,
            _state.Programs,
            _state.Functions,
            null
        );
        var schedulerFactory = new SchedulerFactory(
            _runtimeService.Alarms,
            _runtimeService.Interrupts,
            _runtimeService.Timers,
            _bindingService
        );
        
        // Создаём сервис интерпретаторов
        _interpreterHostingService = new InterpreterHostingService(interpreterFactory, schedulerFactory, _logger);
        
        // Подписка на события
        _lifecycleService.OnProjectChanged += () => OnProjectChanged?.Invoke();
        
        _logger.LogDebug("ProjectManager created with internal services");
    }

    /// <summary>
    /// Инициализирует новый проект с базовыми типами и формами.
    /// </summary>
    public void InitializeNew(string projectFolder)
    {
        _logger.LogInformation("Initializing new project: {Folder}", projectFolder);
        
        // Инициализируем хранилище
        _lifecycleService.InitializeNew(projectFolder);
        
        // Инициализируем типы и формы
        _typeService.InitializePrimitives();
        _typeService.InitializeBuiltinForms();
        
        // Регистрация встроенных функций
        RegisterBuiltinFunctions();

        // Инициализируем runtime
        _runtimeService.Initialize();

        // Инициализируем плагины
        _pluginHostingService.Initialize(projectFolder, _typeService);
        _pluginHostingService.LoadAllPlugins();

        // Обновляем PluginManager в сервисе интерпретаторов
        _interpreterHostingService.UpdatePluginManager(_pluginHostingService.PluginManager);

        _logger.LogInformation("Project initialized successfully");
    }

    /// <summary>
    /// Регистрация встроенных функций.
    /// </summary>
    private void RegisterBuiltinFunctions()
    {
        _state.Functions.Clear();
        var builtins = BuiltinFunctions.GetFunctions();
        foreach (var kv in builtins)
            _state.Functions[kv.Key] = kv.Value;
        _logger.LogDebug("Registered {Count} builtin functions", builtins.Count);
    }

    /// <summary>
    /// Открывает существующий проект из папки.
    /// </summary>
    public void Open(string projectFolder)
    {
        _logger.LogInformation("Opening project: {Folder}", projectFolder);
        _lifecycleService.Open(projectFolder);
        
        // Регистрация встроенных функций
        RegisterBuiltinFunctions();

        _logger.LogInformation("Project opened: {Folder}", projectFolder);
    }

    /// <summary>
    /// Сохраняет весь проект атомарно.
    /// </summary>
    public void SaveAll()
    {
        _logger.LogInformation("Saving all project data");
        _lifecycleService.SaveAll();
    }
        
    /// <summary>
    /// Сохраняет отдельную программу (для автосохранения при изменении).
    /// </summary>
    public void SaveProgram(string programName)
    {
        _logger.LogDebug("Saving program: {ProgramName}", programName);
        _lifecycleService.SaveProgram(programName);
    }
        
    // === Делегирование сервисам ===
    
    /// <summary>
    /// Создаёт интерпретатор для выполнения программы.
    /// </summary>
    public AstroInterpreterEx CreateInterpreter()
    {
        return _interpreterHostingService.CreateInterpreter();
    }

    /// <summary>
    /// Создаёт планировщик задач для многозадачного выполнения.
    /// </summary>
    public AstroScheduler CreateScheduler()
    {
        return _interpreterHostingService.CreateScheduler(
            _state.GlobalTables,
            _state.Programs,
            _typeService.TypeRegistry,
            _typeService.FormRegistry,
            _state.Functions
        );
    }

    // === Методы для работы с программами (делегирование сервису) ===
    public void AddProgram(Programs.AstroProgram program) => _programService.AddProgram(program);
    public bool RemoveProgram(string name) => _programService.RemoveProgram(name);
    public Programs.AstroProgram? GetProgram(string name) => _programService.GetProgram(name);
    public Programs.AstroProgram CreateProgram(string name, string author = "", string description = "") 
        => _programService.CreateProgram(name, author, description);
}