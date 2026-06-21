// AstroEditor.Core/Common/Logging/LoggingService.cs
// Централизованная служба логирования

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Common.Logging;

/// <summary>
/// Служба логирования для AstroEditor.
/// Обёртка над Microsoft.Extensions.Logging для упрощённого использования.
/// </summary>
public class LoggingService
{
    private static ILoggerFactory? _factory;
    private static readonly Dictionary<string, ILogger> _loggers = new();
    private static readonly object _lock = new();
    
    /// <summary>
    /// Инициализирует службу логирования.
    /// </summary>
    public static void Initialize(LogLevel minLevel = LogLevel.Information)
    {
        if (_factory != null) return;
        
        _factory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(minLevel)
                .AddConsole();
        });
    }
    
    /// <summary>
    /// Получает логгер для указанного типа.
    /// </summary>
    public static ILogger GetLogger<T>()
    {
        lock (_lock)
        {
            var typeName = typeof(T).FullName ?? "Unknown";
            
            if (!_loggers.TryGetValue(typeName, out var logger))
            {
                _factory ??= LoggerFactory.Create(builder => 
                    builder.SetMinimumLevel(LogLevel.Information).AddConsole());
                
                logger = _factory.CreateLogger(typeName);
                _loggers[typeName] = logger;
            }
            
            return logger;
        }
    }
    
    /// <summary>
    /// Получает логгер по имени категории.
    /// </summary>
    public static ILogger GetLogger(string category)
    {
        lock (_lock)
        {
            if (!_loggers.TryGetValue(category, out var logger))
            {
                _factory ??= LoggerFactory.Create(builder => 
                    builder.SetMinimumLevel(LogLevel.Information).AddConsole());
                
                logger = _factory.CreateLogger(category);
                _loggers[category] = logger;
            }
            
            return logger;
        }
    }
    
    /// <summary>
    /// Очищает все логгеры.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _loggers.Clear();
            _factory?.Dispose();
            _factory = null;
        }
    }
}

/// <summary>
/// Статический помощник для быстрого доступа к логированию.
/// </summary>
public static class Log
{
    private static readonly ConcurrentDictionary<Type, ILogger> _cache = new();
    
    public static ILogger For<T>() => _cache.GetOrAdd(typeof(T), t => LoggingService.GetLogger<T>());
    
    public static ILogger For(string category) => LoggingService.GetLogger(category);
}
