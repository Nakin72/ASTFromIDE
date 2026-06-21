// AstroEditor.Core/Expressions/ExpressionCache.cs
// Кэширование AST выражений для повышения производительности

using System.Collections.Concurrent;
using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Expressions;

/// <summary>
/// Кэш AST выражений.
/// Избегает повторного парсинга одних и тех же выражений.
/// ✅ P2: Реализует IDisposable для очистки ресурсов.
/// </summary>
public class ExpressionCache : IExpressionCacheDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, CachedExpression> _cache = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly ExpressionParser _parser;
    
    // ✅ P2: Флаг для IDisposable
    private bool _disposed;
    
    // Максимальный размер кэша для предотвращения утечек памяти
    // ✅ P1-3: Разумный лимит по умолчанию (1000 выражений)
    public int MaxSize { get; set; } = 1000;
    
    // Статистика — используем Interlocked для потокобезопасности
    private int _hits;
    private int _misses;
    private int _invalidations;
    
    // Lock для атомарных операций чтения/сброса статистики
    private readonly object _statsLock = new();
    
    public ExpressionCache(ExpressionParser? parser = null, ILogger? logger = null)
    {
        _parser = parser ?? new ExpressionParser();
        _logger = logger ?? Log.For<ExpressionCache>();
    }
    
    /// <summary>
    /// Получить или создать AST для выражения.
    /// </summary>
    public ExpressionNode GetOrParse(string expression)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be empty", nameof(expression));
        
        // Нормализуем выражение (убираем лишние пробелы)
        var normalized = NormalizeExpression(expression);
        
        if (_cache.TryGetValue(normalized, out var cached))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogTrace("Cache HIT: {Expression}", expression);
            return cached.Ast;
        }
        
        // Miss - парсим выражение
        Interlocked.Increment(ref _misses);
        _logger.LogTrace("Cache MISS, parsing: {Expression}", expression);
        
        try
        {
            var ast = _parser.Parse(normalized);
            var cachedExpr = new CachedExpression
            {
                Ast = ast,
                Expression = normalized,
                CachedAt = DateTime.UtcNow,
                AccessCount = 1
            };
            
            _cache[normalized] = cachedExpr;
            _insertionOrder.Enqueue(normalized);
            _logger.LogDebug("Expression cached: {Expression}", expression);
            
            // Автоматическая очистка при превышении лимита
            if (MaxSize > 0 && _cache.Count > MaxSize)
            {
                EvictOldEntries();
            }
            
            return ast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse expression: {Expression}", expression);
            throw;
        }
    }
    
    /// <summary>
    /// Предварительно закэшировать выражения (прекомпиляция).
    /// </summary>
    public void PreCache(IEnumerable<string> expressions)
    {
        foreach (var expr in expressions)
        {
            try
            {
                GetOrParse(expr);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to pre-cache expression: {Expression}", expr);
            }
        }
        
        _logger.LogInformation("Pre-cached {Count} expressions", _cache.Count);
    }
    
    /// <summary>
    /// Очистить кэш для конкретного выражения.
    /// </summary>
    public void Invalidate(string expression)
    {
        var normalized = NormalizeExpression(expression);
        
        lock (_statsLock)
        {
            if (_cache.TryRemove(normalized, out _))
            {
                Interlocked.Increment(ref _invalidations);
                _logger.LogDebug("Cache invalidated: {Expression}", expression);
            }
        }
    }
        
    /// <summary>
    /// Очистить весь кэш.
    /// </summary>
    public void Clear()
    {
        int count;
        int hits, misses, invalidations;
        
        lock (_statsLock)
        {
            count = _cache.Count;
            hits = _hits;
            misses = _misses;
            invalidations = _invalidations;
            
            _cache.Clear();
            ResetStatistics();
        }
        
        _logger.LogInformation("Expression cache cleared ({Count} entries, {Hits} hits, {Misses} misses)", 
            count, hits, misses);
    }
    
    /// <summary>
    /// Очистить старые записи (по времени последнего доступа).
    /// </summary>
    public void TrimOldEntries(TimeSpan maxAge)
    {
        var now = DateTime.UtcNow;
        
        lock (_statsLock)
        {
            var toRemove = _cache
                .Where(kvp => now - kvp.Value.CachedAt > maxAge)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in toRemove)
            {
                _cache.TryRemove(key, out _);
            }
            
            if (toRemove.Count > 0)
            {
                Interlocked.Add(ref _invalidations, toRemove.Count);
                _logger.LogDebug("Trimmed {Count} old cache entries", toRemove.Count);
            }
        }
    }
    
    /// <summary>
    /// Статистика кэша.
    /// </summary>
    public ExpressionCacheStatistics GetStatistics()
    {
        int hits, misses, invalidations;
        int size;
        
        lock (_statsLock)
        {
            hits = _hits;
            misses = _misses;
            invalidations = _invalidations;
            size = _cache.Count;
        }
        
        var total = hits + misses;
        return new ExpressionCacheStatistics
        {
            Size = size,
            Hits = hits,
            Misses = misses,
            HitRate = total > 0 ? (double)hits / total : 0,
            Invalidations = invalidations
        };
    }
    
    private void ResetStatistics()
    {
        _hits = 0;
        _misses = 0;
        _invalidations = 0;
    }
    
    /// <summary>
    /// Удаляет старые записи при превышении лимита размера.
    /// </summary>
    private void EvictOldEntries()
    {
        int evicted = 0;
        while (_cache.Count > MaxSize && _insertionOrder.TryDequeue(out var oldestKey))
        {
            if (_cache.TryRemove(oldestKey, out _))
            {
                evicted++;
                Interlocked.Increment(ref _invalidations);
            }
            // Пропускаем дубликаты в очереди (если ключ уже удалён)
        }
        
        if (evicted > 0)
        {
            _logger.LogDebug("Evicted {Count} old cache entries due to size limit ({MaxSize})", evicted, MaxSize);
        }
    }
    
    private static string NormalizeExpression(string expression)
    {
        // Простая нормализация - убираем лишние пробелы
        return string.Join(" ", expression.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries));
    }
    
    #region IDisposable
    
    /// <summary>
    /// Освободить ресурсы кэша.
    /// ✅ P2: Реализация IDisposable паттерна.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            // ✅ Очищаем кэш при disposal
            Clear();
            _logger.LogDebug("ExpressionCache disposed");
        }
        
        _disposed = true;
    }
    
    ~ExpressionCache()
    {
        Dispose(false);
    }
    
    /// <summary>
    /// Проверить, не disposed ли объект.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ExpressionCache));
    }
    
    #endregion
}

/// <summary>
/// Интерфейс кэша выражений.
/// </summary>
public interface IExpressionCache
{
    ExpressionNode GetOrParse(string expression);
    void PreCache(IEnumerable<string> expressions);
    void Invalidate(string expression);
    void Clear();
    ExpressionCacheStatistics GetStatistics();
}

/// <summary>
/// Расширенный интерфейс кэша выражений с поддержкой IDisposable.
/// </summary>
public interface IExpressionCacheDisposable : IExpressionCache, IDisposable
{
}

/// <summary>
/// Статистика кэша выражений.
/// </summary>
public class ExpressionCacheStatistics
{
    public int Size { get; init; }
    public int Hits { get; init; }
    public int Misses { get; init; }
    public double HitRate { get; init; }
    public int Invalidations { get; init; }
    
    public override string ToString() => 
        $"Size={Size}, Hits={Hits}, Misses={Misses}, HitRate={HitRate:P1}, Invalidations={Invalidations}";
}

/// <summary>
/// Кэшированное выражение.
/// </summary>
internal class CachedExpression
{
    public required ExpressionNode Ast { get; init; }
    public required string Expression { get; init; }
    public required DateTime CachedAt { get; init; }
    public int AccessCount { get; set; }
}
