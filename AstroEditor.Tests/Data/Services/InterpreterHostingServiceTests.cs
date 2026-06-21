// AstroEditor.Tests/Data/Services/InterpreterHostingServiceTests.cs
// Тесты сервиса создания интерпретаторов и планировщиков

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Binding;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Data.Services;

public class InterpreterHostingServiceTests
{
    private readonly ProjectState _state;
    private readonly InterpreterFactory _interpreterFactory;
    private readonly SchedulerFactory _schedulerFactory;
    private readonly InterpreterHostingService _service;

    public InterpreterHostingServiceTests()
    {
        _state = new ProjectState();
        _interpreterFactory = new InterpreterFactory(
            _state.TypeRegistry,
            _state.FormRegistry,
            _state.GlobalTables,
            _state.Programs,
            _state.Functions,
            null
        );
        _schedulerFactory = new SchedulerFactory(
            new AlarmManager(),
            new InterruptManager(),
            new TimerManager(),
            new ThreadSafeBindingManager(_state.GlobalTables)
        );
        _service = new InterpreterHostingService(_interpreterFactory, _schedulerFactory, NullLogger<InterpreterHostingService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Act & Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public void CreateInterpreter_ShouldReturnNotNull()
    {
        // Act
        var interpreter = _service.CreateInterpreter();

        // Assert
        Assert.NotNull(interpreter);
    }

    [Fact]
    public void CreateInterpreter_ShouldReturnNewInstance()
    {
        // Act
        var interpreter1 = _service.CreateInterpreter();
        var interpreter2 = _service.CreateInterpreter();

        // Assert
        Assert.NotSame(interpreter1, interpreter2);
    }

    [Fact]
    public void CreateScheduler_ShouldReturnNotNull()
    {
        // Act
        var scheduler = _service.CreateScheduler(
            _state.GlobalTables,
            _state.Programs,
            _state.TypeRegistry,
            _state.FormRegistry,
            _state.Functions
        );

        // Assert
        Assert.NotNull(scheduler);
    }

    [Fact]
    public void CreateScheduler_ShouldReturnNewInstance()
    {
        // Act
        var scheduler1 = _service.CreateScheduler(
            _state.GlobalTables,
            _state.Programs,
            _state.TypeRegistry,
            _state.FormRegistry,
            _state.Functions
        );
        var scheduler2 = _service.CreateScheduler(
            _state.GlobalTables,
            _state.Programs,
            _state.TypeRegistry,
            _state.FormRegistry,
            _state.Functions
        );

        // Assert
        Assert.NotSame(scheduler1, scheduler2);
    }

    [Fact]
    public void UpdatePluginManager_ShouldNotThrow()
    {
        // Act & Assert
        var ex = Record.Exception(() => _service.UpdatePluginManager(null));
        Assert.Null(ex);
    }

    [Fact]
    public void CreateInterpreter_ShouldHaveExpressionCache()
    {
        // Act
        var interpreter = _service.CreateInterpreter();

        // Assert
        var cache = interpreter.GetExpressionCache();
        Assert.NotNull(cache);
    }
}
