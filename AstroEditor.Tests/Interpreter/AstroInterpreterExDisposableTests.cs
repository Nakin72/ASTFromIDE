// AstroEditor.Tests/Interpreter/AstroInterpreterExDisposableTests.cs
// Тесты IDisposable для AstroInterpreterEx

using Xunit;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Interpreter;

public class AstroInterpreterExDisposableTests
{
    private readonly InterpreterContext _context;

    public AstroInterpreterExDisposableTests()
    {
        _context = new InterpreterContext
        {
            TypeRegistry = new DataTypeRegistry(),
            FormRegistry = new FormRegistry(),
            GlobalTables = new VariableTableSet { Name = "Global", IsGlobal = true },
            Functions = new Dictionary<string, Func<object?[], object?>>(),
            ProgramRegistry = new Dictionary<string, AstroProgram>()
        };
    }

    [Fact]
    public void AstroInterpreterEx_Implements_IDisposable()
    {
        // Arrange
        var interpreter = new AstroInterpreterEx(_context);

        // Act & Assert
        Assert.IsAssignableFrom<IDisposable>(interpreter);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var interpreter = new AstroInterpreterEx(_context);

        // Act & Assert
        var ex = Record.Exception(() => interpreter.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var interpreter = new AstroInterpreterEx(_context);

        // Act & Assert
        var ex1 = Record.Exception(() => interpreter.Dispose());
        var ex2 = Record.Exception(() => interpreter.Dispose());
        
        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact]
    public void Dispose_ShouldStopExecution()
    {
        // Arrange
        var interpreter = new AstroInterpreterEx(_context);

        // Act
        interpreter.Dispose();

        // Assert
        Assert.False(interpreter.IsRunning);
    }

    [Fact]
    public void Using_Statement_ShouldDispose()
    {
        // Arrange & Act
        AstroInterpreterEx interpreter;
        using (interpreter = new AstroInterpreterEx(_context))
        {
            // interpreter используется
        }

        // Assert - dispose вызван автоматически
        // Проверяем через повторный dispose (не должен выбросить)
        var ex = Record.Exception(() => interpreter.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ShouldResetState()
    {
        // Arrange
        var interpreter = new AstroInterpreterEx(_context);

        // Act
        interpreter.Dispose();

        // Assert - состояние должно быть сброшено
        Assert.Null(interpreter.State?.Program);
    }

    [Fact]
    public void Dispose_WithExpressionCache_ShouldDisposeCache()
    {
        // Arrange
        var expressionCache = new ExpressionCache();
        var interpreter = new AstroInterpreterEx(_context, expressionCache: expressionCache);

        // Act
        interpreter.Dispose();

        // Assert - кэш должен быть очищен
        var stats = expressionCache.GetStatistics();
        Assert.Equal(0, stats.Size);
    }
}
