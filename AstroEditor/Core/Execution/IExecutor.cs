// AstroEditor/Core/Execution/IExecutor.cs
// Интерфейсы слоя исполнения и многозадачности

using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;

namespace AstroEditor.Core.Execution;

/// <summary>
/// Состояние выполнения одной задачи.
/// </summary>
public class TaskState
{
    public int TaskId { get; init; }
    public string Name { get; set; } = string.Empty;
    public AstroProgram Program { get; set; } = null!;
    public Tables.VariableTableSet LocalTables { get; set; } = null!;
    public int CurrentLineIndex { get; set; }
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool StopRequested { get; set; }
    public object? ReturnValue { get; set; }
    public Stack<CallFrame> CallStack { get; } = new();
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Фрейм вызова подпрограммы.
/// </summary>
public class CallFrame
{
    public AstroProgram Program { get; init; } = null!;
    public int ReturnLineIndex { get; init; }
    public Tables.VariableTableSet LocalTables { get; init; } = null!;
}

/// <summary>
/// Приоритет задачи.
/// </summary>
public enum TaskPriority
{
    Critical = 0,   // Аварийные/защитные программы
    High = 1,       // Основные задачи управления
    Normal = 2,     // Пользовательские программы
    Low = 3,        // Фоновые задачи
    Idle = 4        // Задачи, выполняемые в простое
}

/// <summary>
/// Тип задачи.
/// </summary>
public enum TaskType
{
    Foreground,     // Основная программа
    Background,     // Фоновая (циклическая) программа
    EventDriven     // По событию/прерыванию
}

/// <summary>
/// Конфигурация задачи.
/// </summary>
public class TaskConfig
{
    public int TaskId { get; init; }
    public string Name { get; set; } = string.Empty;
    public AstroProgram Program { get; init; } = null!;
    public TaskType Type { get; init; } = TaskType.Foreground;
    public TaskPriority Priority { get; init; } = TaskPriority.Normal;
    public int CycleIntervalMs { get; init; } = 0; // для background: период выполнения
    public int MaxCycles { get; init; } = 0;       // 0 = бесконечно
    public CancellationToken? CancellationToken { get; init; }
}

/// <summary>
/// Планировщик задач. Управляет жизненным циклом и переключением контекста.
/// </summary>
public interface ITaskScheduler
{
    /// <summary>Создаёт и запускает новую задачу</summary>
    TaskState StartTask(TaskConfig config);
    
    /// <summary>Приостанавливает задачу</summary>
    void PauseTask(int taskId);
    
    /// <summary>Возобновляет задачу</summary>
    void ResumeTask(int taskId);
    
    /// <summary>Останавливает задачу</summary>
    void StopTask(int taskId);
    
    /// <summary>Останавливает все задачи</summary>
    void StopAll();
    
    /// <summary>Возвращает состояние задачи</summary>
    TaskState? GetTaskState(int taskId);
    
    /// <summary>Список всех задач</summary>
    IReadOnlyList<TaskState> GetAllTasks();
    
    /// <summary>Выполнить один шаг (step) для указанной задачи</summary>
    void StepTask(int taskId);
    
    /// <summary>Запускает цикл планировщика (фоновый поток)</summary>
    void StartScheduler();
    
    /// <summary>Останавливает планировщик</summary>
    void StopScheduler();
}

/// <summary>
/// Исполнитель инструкций (один шаг программы).
/// </summary>
public interface IExecutor
{
    /// <summary>Выполнить одну инструкцию в контексте задачи</summary>
    void ExecuteOne(TaskState task);
    
    /// <summary>Выполнить программу до конца (синхронно)</summary>
    void ExecuteAll(TaskState task);
}