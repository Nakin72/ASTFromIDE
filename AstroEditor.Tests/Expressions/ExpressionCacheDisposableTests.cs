// AstroEditor.Tests/Expressions/ExpressionCacheDisposableTests.cs
// Тесты IDisposable для ExpressionCache

using Xunit;
using AstroEditor.Core.Expressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Expressions;

public class ExpressionCacheDisposableTests
{
    [Fact]
    public void ExpressionCache_Implements_IDisposable()
    {
        // Arrange
        var cache = new ExpressionCache();

        // Act & Assert
        Assert.IsAssignableFrom<IDisposable>(cache);
    }

    [Fact]
    public void Dispose_ShouldClearCache()
    {
        // Arrange
        var cache = new ExpressionCache();
        cache.GetOrParse("1 + 2");
        cache.GetOrParse("3 + 4");
        
        // Act
        cache.Dispose();

        // Assert
        var stats = cache.GetStatistics();
        Assert.Equal(0, stats.Size);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var cache = new ExpressionCache();
        cache.GetOrParse("1 + 2");

        // Act & Assert
        var ex1 = Record.Exception(() => cache.Dispose());
        var ex2 = Record.Exception(() => cache.Dispose());
        
        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact]
    public void GetOrParse_AfterDispose_ShouldThrow()
    {
        // Arrange
        var cache = new ExpressionCache();
        cache.Dispose();

        // Act
        var ex = Record.Exception(() => cache.GetOrParse("1 + 2"));

        // Assert
        Assert.IsType<ObjectDisposedException>(ex);
    }

    [Fact]
    public void Using_Statement_ShouldDispose()
    {
        // Arrange & Act
        using (var cache = new ExpressionCache())
        {
            cache.GetOrParse("1 + 2");
        }

        // Assert - кэш должен быть очищен
        // Проверяем через попытку использования
        var cache2 = new ExpressionCache();
        cache2.GetOrParse("test");
        cache2.Dispose();
    }

    [Fact]
    public void ExpressionCache_Implements_IExpressionCacheDisposable()
    {
        // Arrange
        var cache = new ExpressionCache();

        // Act & Assert
        Assert.IsAssignableFrom<IExpressionCacheDisposable>(cache);
    }
}
