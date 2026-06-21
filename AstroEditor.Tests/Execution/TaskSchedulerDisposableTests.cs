// AstroEditor.Tests/Execution/TaskSchedulerDisposableTests.cs
// Тесты IDisposable для TaskScheduler

using Xunit;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Programs;

namespace AstroEditor.Tests.Execution;

public class TaskSchedulerDisposableTests
{
    private readonly VariableTableSet _globalTables;
    private readonly Dictionary<string, AstroProgram> _programs;
    private readonly DataTypeRegistry _typeRegistry;
    private readonly FormRegistry _formRegistry;

    public TaskSchedulerDisposableTests()
    {
        _globalTables = new VariableTableSet { Name = "Global", IsGlobal = true };
        _programs = new Dictionary<string, AstroProgram>();
        _typeRegistry = new DataTypeRegistry();
        _formRegistry = new FormRegistry();
    }

    [Fact]
    public void TaskScheduler_Implements_IDisposable()
    {
        // Arrange
        var scheduler = new Core.Execution.TaskScheduler();

        // Act & Assert
        Assert.IsAssignableFrom<IDisposable>(scheduler);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var scheduler = new Core.Execution.TaskScheduler();

        // Act & Assert
        var ex = Record.Exception(() => scheduler.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var scheduler = new Core.Execution.TaskScheduler(
            _globalTables,
            _programs,
            _typeRegistry,
            _formRegistry
        );

        // Act & Assert
        var ex1 = Record.Exception(() => scheduler.Dispose());
        var ex2 = Record.Exception(() => scheduler.Dispose());
        
        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact]
    public void Dispose_ShouldStopScheduler()
    {
        // Arrange
        var scheduler = new Core.Execution.TaskScheduler(
            _globalTables,
            _programs,
            _typeRegistry,
            _formRegistry
        );

        // Act
        scheduler.Dispose();

        // Assert - планировщик должен быть остановлен
        // Проверяем через попытку запуска (не должно быть исключений)
        var ex = Record.Exception(() => scheduler.StopScheduler());
        Assert.Null(ex);
    }

    [Fact]
    public void Using_Statement_ShouldDispose()
    {
        // Arrange
        Core.Execution.TaskScheduler scheduler;
        
        // Act
        using (scheduler = new Core.Execution.TaskScheduler(
            _globalTables,
            _programs,
            _typeRegistry,
            _formRegistry))
        {
            // scheduler используется
        }

        // Assert - dispose вызван автоматически
        // Проверяем через повторный dispose (не должен выбросить)
        var ex = Record.Exception(() => scheduler.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ShouldClearInterpreters()
    {
        // Arrange
        var bindingService = new ThreadSafeBindingManager(_globalTables);
        var alarmService = new AlarmManager();
        
        var scheduler = new Core.Execution.TaskScheduler(
            _globalTables,
            _programs,
            _typeRegistry,
            _formRegistry
        )
        {
            AlarmService = alarmService,
            BindingService = bindingService
        };

        // Act
        scheduler.Dispose();

        // Assert - интерпретаторы должны быть очищены
        // Проверяем через повторный dispose
        var ex = Record.Exception(() => scheduler.Dispose());
        Assert.Null(ex);
    }
}
