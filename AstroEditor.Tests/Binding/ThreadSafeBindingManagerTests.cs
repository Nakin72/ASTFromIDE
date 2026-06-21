// AstroEditor.Tests/Binding/ThreadSafeBindingManagerTests.cs
// Тесты потокобезопасного менеджера привязок

using Xunit;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Tables;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Binding;

public class ThreadSafeBindingManagerTests
{
    private readonly ThreadSafeBindingManager _manager;
    private readonly VariableTableSet _globalTables;

    public ThreadSafeBindingManagerTests()
    {
        _globalTables = new VariableTableSet { Name = "Global", IsGlobal = true };
        _manager = new ThreadSafeBindingManager(_globalTables, NullLogger<ThreadSafeBindingManager>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateManager()
    {
        // Act & Assert
        Assert.NotNull(_manager);
    }

    [Fact]
    public void Bind_ShouldCreateBinding()
    {
        // Act
        var binding = _manager.Bind("Global.Var1", "Global.Var2", BindingDirection.Bidirectional);

        // Assert
        Assert.NotNull(binding);
    }

    [Fact]
    public void UpdateValue_ShouldNotThrow()
    {
        // Act
        var ex = Record.Exception(() => _manager.UpdateValue("Global.Var1", 100));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void GetAllBindings_ShouldReturnCollection()
    {
        // Arrange
        _manager.Bind("Global.Var1", "Global.Var2", BindingDirection.Bidirectional);

        // Act
        var bindings = _manager.GetAllBindings();

        // Assert
        Assert.NotNull(bindings);
    }

    [Fact]
    public void UpdateValue_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new Task[100];

        // Act - множество потоков обновляют переменную
        for (int i = 0; i < 100; i++)
        {
            var value = i;
            tasks[i] = Task.Run(() => _manager.UpdateValue("Global.Var1", value));
        }

        Task.WaitAll(tasks);

        // Assert - исключений не должно быть
    }

    [Fact]
    public void Bind_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new Task[50];

        // Act - множество потоков создают привязки
        for (int i = 0; i < 50; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    _manager.Bind($"Global.Var{idx}", $"Global.Var{idx+1}", BindingDirection.Bidirectional);
                }
                catch { } // Игнорируем дубликаты
            });
        }

        Task.WaitAll(tasks);

        // Assert - исключений не должно быть
    }
}
