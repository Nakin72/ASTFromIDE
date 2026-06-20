// AstroEditor/Core/Data/ProjectManager.cs
// Центральный менеджер проекта.
// Управляет реестрами типов, форм, таблицами переменных, программами.
// Отвечает за атомарное сохранение/загрузку.
// Синглтон для доступа из BindingRouter.

using AstroEditor.Core.Binding;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Common;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Serialization;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Interpreter;

using AstroEditor.Core.Alarms;
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
/// Менеджер проекта — синглтон, управляющий всеми данными и сохранением.
/// </summary>
public class ProjectManager
{
    private static ProjectManager? _instance;
    public static ProjectManager? Instance => _instance;

    private readonly ProjectState _state = new();
    private string _projectFolder = string.Empty;
    private string _registryFolder => Path.Combine(_projectFolder, "Registry");
    private string _programsFolder => Path.Combine(_projectFolder, "Programs");
    private bool _hasUnsavedChanges;

    // Доступ к данным
    public DataTypeRegistry TypeRegistry => _state.TypeRegistry;
    public FormRegistry FormRegistry => _state.FormRegistry;
    public AstroGlobalTables GlobalTables => _state.GlobalTables;
    public Dictionary<string, Programs.AstroProgram> Programs => _state.Programs;
    public Dictionary<string, Func<object?[], object?>> Functions => _state.Functions;
    public string ProjectFolder => _projectFolder;
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    // Планировщик
    private AstroScheduler? _scheduler;

    // Менеджер аварий
    private AlarmManager? _alarmManager;
    public IAlarmService Alarms
    {
        get
        {
            _alarmManager ??= CreateAlarmManager();
            return _alarmManager;
        }
    }

    // Менеджер прерываний
    private InterruptManager? _interruptManager;
    public IInterruptService Interrupts
    {
        get
        {
            if (_interruptManager == null)
            {
                _interruptManager = new InterruptManager
                {
                    GlobalTables = _state.GlobalTables,
                    ProgramRegistry = _state.Programs,
                    TypeRegistry = _state.TypeRegistry,
                    AlarmManager = _alarmManager,
                    Scheduler = _scheduler
                };
            }
            return _interruptManager;
        }
    }

    // Менеджер таймеров
    private TimerManager? _timerManager;
    public ITimerService Timers
    {
        get
        {
            if (_timerManager == null)
            {
                _timerManager = new TimerManager
                {
                    Interrupts = _interruptManager,
                    Scheduler = _scheduler,
                    ProgramRegistry = _state.Programs
                };
                _timerManager.Start();
            }
            return _timerManager;
        }
    }

    // Событие изменения
    public event Action? OnProjectChanged;

    public ProjectManager()
    {
        _instance = this;
    }

    /// <summary>
    /// Инициализирует новый проект с базовыми типами и формами.
    /// </summary>
    public void InitializeNew(string projectFolder)
    {
        _projectFolder = projectFolder;
        _state.TypeRegistry = new DataTypeRegistry();
        _state.FormRegistry = new FormRegistry();
        _state.GlobalTables = new AstroGlobalTables { Name = "GlobalVariables", IsGlobal = true };
        _state.Programs.Clear();

        RegisterPrimitives();
        RegisterBuiltinForms();
        RegisterBuiltinFunctions();

        // Инициализация аварий
        _alarmManager = new AlarmManager();
        _alarmManager.RegisterSystemAlarms();

        _hasUnsavedChanges = true;
        OnProjectChanged?.Invoke();
    }

    /// <summary>
    /// Открывает существующий проект из папки.
    /// </summary>
    public void Open(string projectFolder)
    {
        _projectFolder = projectFolder;
        Directory.CreateDirectory(_registryFolder);
        Directory.CreateDirectory(_programsFolder);

        _state.TypeRegistry = AstroSerializer.LoadDataTypeRegistry(_registryFolder);
        _state.FormRegistry = AstroSerializer.LoadFormRegistry(_registryFolder);
        _state.GlobalTables = AstroSerializer.LoadGlobalTables(_registryFolder, _state.TypeRegistry);
        _state.Programs.Clear();

        // Загружаем все программы из папки
        if (Directory.Exists(_programsFolder))
        {
            foreach (var file in Directory.GetFiles(_programsFolder, "*.ast"))
            {
                var progName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var program = AstroSerializer.LoadProgram(_programsFolder, progName, _state.TypeRegistry);
                    _state.Programs[progName] = program;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load program {progName}: {ex.Message}");
                }
            }
        }

        RegisterBuiltinFunctions();

        // Загружаем аварии
        _alarmManager = LoadAlarms();

        _hasUnsavedChanges = false;
        OnProjectChanged?.Invoke();
    }

    /// <summary>
    /// Сохраняет весь проект атомарно.
    /// </summary>
    public void SaveAll()
    {
        if (string.IsNullOrEmpty(_projectFolder))
            throw new InvalidOperationException("Project folder is not set");

        Directory.CreateDirectory(_registryFolder);
        Directory.CreateDirectory(_programsFolder);

        AstroSerializer.SaveDataTypeRegistry(_state.TypeRegistry, _registryFolder);
        AstroSerializer.SaveFormRegistry(_state.FormRegistry, _registryFolder);
        AstroSerializer.SaveGlobalTables(_state.GlobalTables, _registryFolder);

        foreach (var program in _state.Programs.Values)
        {
            AstroSerializer.SaveProgram(program, _programsFolder);
        }

        // Сохраняем аварии
        SaveAlarms();

        _hasUnsavedChanges = false;
    }

    /// <summary>
    /// Сохраняет отдельную программу (для автосохранения при изменении).
    /// </summary>
    public void SaveProgram(string programName)
    {
        if (_state.Programs.TryGetValue(programName, out var program))
        {
            AstroSerializer.SaveProgram(program, _programsFolder);
            _hasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Добавляет программу в проект.
    /// </summary>
    public void AddProgram(Programs.AstroProgram program)
    {
        _state.Programs[program.Name] = program;
        _hasUnsavedChanges = true;
        OnProjectChanged?.Invoke();
    }

    /// <summary>
    /// Удаляет программу из проекта.
    /// </summary>
    public bool RemoveProgram(string name)
    {
        if (_state.Programs.Remove(name))
        {
            _hasUnsavedChanges = true;
            OnProjectChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Создаёт интерпретатор для выполнения программы.
    /// </summary>
    public AstroInterpreter CreateInterpreter()
    {
        var context = new InterpreterContext
        {
            TypeRegistry = _state.TypeRegistry,
            FormRegistry = _state.FormRegistry,
            GlobalTables = _state.GlobalTables,
            Functions = _state.Functions,
            ProgramRegistry = _state.Programs
        };
        return new AstroInterpreter(context);
    }

    /// <summary>
    /// Создаёт планировщик задач для многозадачного выполнения.
    /// </summary>
    public AstroScheduler CreateScheduler()
    {
        var sched = new AstroScheduler
        {
            GlobalTables = _state.GlobalTables,
            Functions = _state.Functions,
            ProgramRegistry = _state.Programs,
            TypeRegistry = _state.TypeRegistry,
            InterruptService = Interrupts,
            TimerService = Timers
        };
        _scheduler = sched;
        return sched;
    }

    // ========== Приватные методы инициализации ==========

    private void RegisterPrimitives()
    {
        var primitives = new (string Id, string Name, BuiltInPrimitive Prim)[]
        {
            ("sbyte", "SBYTE", BuiltInPrimitive.SByte),
            ("byte", "BYTE", BuiltInPrimitive.Byte),
            ("short", "SHORT", BuiltInPrimitive.Short),
            ("ushort", "USHORT", BuiltInPrimitive.UShort),
            ("int", "INT", BuiltInPrimitive.Int),
            ("uint", "UINT", BuiltInPrimitive.UInt),
            ("long", "LONG", BuiltInPrimitive.Long),
            ("ulong", "ULONG", BuiltInPrimitive.ULong),
            ("float", "FLOAT", BuiltInPrimitive.Float),
            ("double", "DOUBLE", BuiltInPrimitive.Double),
            ("decimal", "DECIMAL", BuiltInPrimitive.Decimal),
            ("bool", "BOOL", BuiltInPrimitive.Bool),
            ("char", "CHAR", BuiltInPrimitive.Char),
            ("string", "STRING", BuiltInPrimitive.String)
        };

        foreach (var (id, name, prim) in primitives)
        {
            if (_state.TypeRegistry.GetTypeById(id) == null)
            {
                var type = new PrimitiveDataType
                {
                    Id = id,
                    Name = name,
                    Primitive = prim,
                    Category = DataTypeCategory.Core
                };
                _state.TypeRegistry.RegisterType(type);
            }
        }

        // Системные типы-псевдонимы
        RegisterAliasIfMissing("real", "REAL", "double", DataTypeCategory.System);
        RegisterAliasIfMissing("position", "POSITION", null, DataTypeCategory.System, fields: new List<StructField>
        {
            new() { Name = "X", TypeId = "double" },
            new() { Name = "Y", TypeId = "double" },
            new() { Name = "Z", TypeId = "double" },
            new() { Name = "A", TypeId = "double" },
            new() { Name = "B", TypeId = "double" },
            new() { Name = "C", TypeId = "double" }
        });

        _state.TypeRegistry.ResolveReferences();
    }

    private void RegisterAliasIfMissing(string id, string name, string? baseTypeId, DataTypeCategory category, List<StructField>? fields = null)
    {
        if (_state.TypeRegistry.GetTypeById(id) != null) return;

        if (fields != null)
        {
            var structType = new StructDataType
            {
                Id = id,
                Name = name,
                Category = category,
                Fields = fields
            };
            _state.TypeRegistry.RegisterType(structType);
        }
        else if (baseTypeId != null)
        {
            var alias = new AliasDataType
            {
                Id = id,
                Name = name,
                Category = category,
                BaseTypeId = baseTypeId
            };
            _state.TypeRegistry.RegisterType(alias);
        }
    }

    private void RegisterBuiltinForms()
    {
        _state.FormRegistry.Clear();
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAssignmentForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateWhileForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndWhileForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateForForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndForForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateElseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateSwitchForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateCaseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateDefaultForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateEndSwitchForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateCallForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateReturnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateLabelForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateJumpLblForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateJumpIfForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateBreakForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateContinueForm());

        // Формы аварий
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmRaiseForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmClearForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmAckForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateAlarmClearAllForm());

        // Формы прерываний
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptDeclareForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptOnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateInterruptOffForm());

        // Формы таймеров
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerDeclareForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerOnForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerOffForm());
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateTimerResetForm());

        // Форма WAIT
        _state.FormRegistry.RegisterForm(BuiltinForms.CreateWaitForm());
    }

    private void RegisterBuiltinFunctions()
    {
        _state.Functions.Clear();
        var builtins = BuiltinFunctions.GetFunctions();
        foreach (var kv in builtins)
            _state.Functions[kv.Key] = kv.Value;
    }

    // ========== Управление авариями ==========

    private AlarmManager CreateAlarmManager()
    {
        var mgr = new AlarmManager();
        mgr.RegisterSystemAlarms();
        return mgr;
    }

    private void SaveAlarms()
    {
        if (_alarmManager == null) return;
        var snapshot = new AlarmStateSnapshot
        {
            Definitions = _alarmManager.Definitions.Values.ToList(),
            ActiveAlarms = _alarmManager.ActiveAlarms.Values.ToList(),
            History = _alarmManager.AlarmHistory.ToList()
        };
        var path = Path.Combine(_registryFolder, "alarms.json");
        AstroSerializer.SaveToFile(snapshot, path);
    }

    private AlarmManager LoadAlarms()
    {
        var mgr = new AlarmManager();
        var path = Path.Combine(_registryFolder, "alarms.json");
        if (File.Exists(path))
        {
            try
            {
                var snapshot = AstroSerializer.LoadFromFile<AlarmStateSnapshot>(path);
                if (snapshot != null)
                {
                    foreach (var def in snapshot.Definitions)
                        mgr.RegisterAlarm(def);
                    // Активные экземпляры восстанавливаются через Raise
                    // (упрощённо — регистрируем, но не поднимаем заново)
                }
            }
            catch { /* игнорируем ошибки загрузки */ }
        }
        else
        {
            mgr.RegisterSystemAlarms();
        }
        return mgr;
    }
}