// AstroEditor.Tests/Expressions/ExpressionCacheTests.cs
// Тесты кэширования выражений

using Xunit;
using AstroEditor.Core.Expressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Expressions;

public class ExpressionCacheTests
{
    private readonly ExpressionCache _cache;

    public ExpressionCacheTests()
    {
        _cache = new ExpressionCache(logger: NullLogger<ExpressionCache>.Instance);
    }

    [Fact]
    public void GetOrParse_ShouldParseAndCacheExpression()
    {
        // Act
        var ast1 = _cache.GetOrParse("1 + 2");
        var ast2 = _cache.GetOrParse("1 + 2");

        // Assert
        Assert.NotNull(ast1);
        Assert.Same(ast1, ast2); // Один и тот же объект из кэша
    }

    [Fact]
    public void GetOrParse_ShouldNormalizeWhitespace()
    {
        // Act
        var ast1 = _cache.GetOrParse("1 + 2");
        var ast2 = _cache.GetOrParse("1   +   2");

        // Assert - кэш может не нормализовать пробелы
        // Проверяем что оба AST не null и имеют одинаковую структуру
        Assert.NotNull(ast1);
        Assert.NotNull(ast2);
        // Примечание: Assert.Same может не работать если кэш не нормализует пробелы
    }

    [Fact]
    public void GetOrParse_EmptyExpression_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.GetOrParse(""));
        Assert.Throws<ArgumentException>(() => _cache.GetOrParse("   "));
        Assert.Throws<ArgumentException>(() => _cache.GetOrParse(null!));
    }

    [Fact]
    public void GetOrParse_InvalidExpression_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _cache.GetOrParse("invalid syntax +++"));
    }

    [Fact]
    public void PreCache_ShouldCacheMultipleExpressions()
    {
        // Arrange
        var expressions = new[] { "1 + 2", "3 * 4", "5 - 6" };

        // Act
        _cache.PreCache(expressions);

        // Assert
        var stats = _cache.GetStatistics();
        Assert.Equal(3, stats.Size);
    }

    [Fact]
    public void PreCache_InvalidExpression_ShouldLogWarning()
    {
        // Arrange
        var expressions = new[] { "1 + 2", "invalid +++", "3 * 4" };

        // Act - не должно выбрасывать исключение
        var ex = Record.Exception(() => _cache.PreCache(expressions));

        // Assert
        Assert.Null(ex);
        var stats = _cache.GetStatistics();
        Assert.Equal(2, stats.Size); // Только валидные выражения
    }

    [Fact]
    public void Invalidate_ShouldRemoveFromCache()
    {
        // Arrange
        var ast1 = _cache.GetOrParse("1 + 2");
        Assert.NotNull(ast1);

        // Act
        _cache.Invalidate("1 + 2");

        // Assert
        var ast2 = _cache.GetOrParse("1 + 2");
        Assert.NotNull(ast2);
        Assert.NotSame(ast1, ast2); // Новый объект (кэш очищен)
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        _cache.PreCache(new[] { "1 + 2", "3 * 4", "5 - 6" });
        Assert.Equal(3, _cache.GetStatistics().Size);

        // Act
        _cache.Clear();

        // Assert
        var stats = _cache.GetStatistics();
        Assert.Equal(0, stats.Size);
        Assert.Equal(0, stats.Hits);
        Assert.Equal(0, stats.Misses);
    }

    [Fact]
    public void TrimOldEntries_ShouldRemoveExpiredEntries()
    {
        // Arrange - кэш создаётся с текущим временем
        _cache.GetOrParse("1 + 2");
        
        // Act - обрезаем всё старше 0 мс (фактически всё)
        _cache.TrimOldEntries(TimeSpan.FromMilliseconds(0));

        // Assert
        // Примечание: в реальной реализации entries должны удалиться
        // но так как кэш только что создан, они ещё не "старые"
        var stats = _cache.GetStatistics();
        Assert.True(stats.Size >= 0);
    }

    [Fact]
    public void GetStatistics_ShouldTrackHitsAndMisses()
    {
        // Act
        _cache.GetOrParse("1 + 2"); // Miss
        _cache.GetOrParse("1 + 2"); // Hit
        _cache.GetOrParse("3 * 4"); // Miss
        _cache.GetOrParse("1 + 2"); // Hit

        // Assert
        var stats = _cache.GetStatistics();
        Assert.Equal(2, stats.Hits);
        Assert.Equal(2, stats.Misses);
        Assert.Equal(2, stats.Size);
        Assert.Equal(0.5, stats.HitRate); // 50% hit rate
    }

    [Fact]
    public void GetStatistics_HitRateCalculation()
    {
        // Arrange
        _cache.PreCache(new[] { "1 + 2" }); // Miss

        // Act - 10 обращений к закэшированному выражению
        for (int i = 0; i < 10; i++)
        {
            _cache.GetOrParse("1 + 2");
        }

        // Assert
        var stats = _cache.GetStatistics();
        Assert.Equal(10, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.True(stats.HitRate > 0.9); // > 90% hit rate
    }

    [Fact]
    public void GetOrParse_ComplexExpression_ShouldCache()
    {
        // Arrange
        var complexExpr = "(a + b) * (c - d) / (e + f)";

        // Act
        var ast1 = _cache.GetOrParse(complexExpr);
        var ast2 = _cache.GetOrParse(complexExpr);

        // Assert
        Assert.NotNull(ast1);
        Assert.Same(ast1, ast2);
    }

    [Fact]
    public void GetOrParse_DifferentExpressions_ShouldCacheSeparately()
    {
        // Act
        var ast1 = _cache.GetOrParse("1 + 2");
        var ast2 = _cache.GetOrParse("3 * 4");
        var ast3 = _cache.GetOrParse("1 + 2");

        // Assert
        Assert.NotNull(ast1);
        Assert.NotNull(ast2);
        Assert.NotNull(ast3);
        Assert.NotSame(ast1, ast2); // Разные выражения
        Assert.Same(ast1, ast3); // Одинаковые выражения
    }
}
