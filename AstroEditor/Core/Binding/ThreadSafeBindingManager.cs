// AstroEditor.Core/Binding/ThreadSafeBindingManager.cs
// Потокобезопасный менеджер привязок с кэшированием

using System.Collections.Concurrent;
using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Variables;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Binding;

/// <summary>
/// Потокобезопасный менеджер привязок переменных.
/// Поддерживает одновременное чтение и безопасную запись.
/// </summary>
public class ThreadSafeBindingManager : IBindingService
{
    private readonly ILogger _logger;
    
    // Используем ConcurrentDictionary для потокобезопасности
    private readonly ConcurrentDictionary<string, Binding> _bindings = new();
    
    // Кэш значений для быстрого доступа (избегает лишних поисков переменных)
    private readonly ConcurrentDictionary<string, Variable?> _variableCache = new();
    
    // Lock только для операций обновления значений
    private readonly ReaderWriterLockSlim _updateLock = new();
    
    private readonly VariableTableSet _globalTables;
    
    public ThreadSafeBindingManager(VariableTableSet globalTables, ILogger? logger = null)
    {
        _globalTables = globalTables;
        _logger = logger ?? Log.For<ThreadSafeBindingManager>();
    }
    
    /// <summary>
    /// Создать привязку между переменными.
    /// </summary>
    public Binding Bind(string sourceName, string targetName, BindingDirection direction = BindingDirection.Bidirectional)
    {
        var binding = new Binding
        {
            Id = Guid.NewGuid().ToString(),
            SourceName = sourceName,
            TargetName = targetName,
            Direction = direction,
            CreatedAt = DateTime.UtcNow,
            IsEnabled = true
        };
        
        var key = GetBindingKey(sourceName, targetName);
        _bindings[key] = binding;
        
        // Очищаем кэш переменных, т.к. могли появиться новые связи
        InvalidateVariableCache(sourceName);
        InvalidateVariableCache(targetName);
        
        _logger.LogDebug("Binding created: {Source} {Direction} {Target}", 
            sourceName, GetDirectionSymbol(direction), targetName);
        
        return binding;
    }
    
    private static string GetDirectionSymbol(BindingDirection dir) => dir switch
    {
        BindingDirection.OneWayFromTarget => "<=",
        BindingDirection.OneWayToTarget => "=>",
        BindingDirection.Bidirectional => "<=>",
        _ => "<=>"
    };
    
    /// <summary>
    /// Удалить привязку.
    /// </summary>
    public bool Unbind(string sourceName, string targetName)
    {
        var key = GetBindingKey(sourceName, targetName);
        if (_bindings.TryRemove(key, out var binding))
        {
            InvalidateVariableCache(sourceName);
            InvalidateVariableCache(targetName);
            
            _logger.LogDebug("Binding removed: {Source} <=> {Target}", sourceName, targetName);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Обновить значение переменной с учётом привязок.
    /// Потокобезопасная операция.
    /// </summary>
    public void UpdateValue(string variableName, object? newValue)
    {
        _updateLock.EnterUpgradeableReadLock();
        try
        {
            // Находим переменную
            var variable = GetVariableCached(variableName);
            if (variable == null)
            {
                _logger.LogWarning("Variable not found for update: {VariableName}", variableName);
                return;
            }
            
            // Обновляем значение
            _updateLock.EnterWriteLock();
            try
            {
                variable.Value = newValue;
                _logger.LogTrace("Variable updated: {VariableName} = {Value}", variableName, newValue);
            }
            finally
            {
                _updateLock.ExitWriteLock();
            }
            
            // Применяем привязки
            ApplyBindings(variableName, newValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variable {VariableName}", variableName);
            throw;
        }
        finally
        {
            _updateLock.ExitUpgradeableReadLock();
        }
    }
    
    /// <summary>
    /// Применить привязки для переменной.
    /// </summary>
    private void ApplyBindings(string sourceName, object? value)
    {
        var affectedBindings = _bindings.Values
            .Where(b => b.IsEnabled && 
                   (b.SourceName == sourceName || b.TargetName == sourceName))
            .ToList();
        
        foreach (var binding in affectedBindings)
        {
            try
            {
                var isSource = binding.SourceName == sourceName;
                
                // Определяем, куда распространять значение
                var shouldPropagate = binding.Direction switch
                {
                    BindingDirection.Bidirectional => true,
                    BindingDirection.OneWayToTarget => isSource,
                    BindingDirection.OneWayFromTarget => !isSource,
                    _ => false
                };
                
                if (!shouldPropagate) continue;
                
                var targetName = isSource ? binding.TargetName : binding.SourceName;
                var targetVar = GetVariableCached(targetName);
                
                if (targetVar != null)
                {
                    _updateLock.EnterWriteLock();
                    try
                    {
                        targetVar.Value = value;
                        _logger.LogTrace("Binding propagated: {Source} -> {Target} = {Value}", 
                            sourceName, targetName, value);
                    }
                    finally
                    {
                        _updateLock.ExitWriteLock();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying binding {BindingId}", binding.Id);
            }
        }
    }
    
    /// <summary>
    /// Получить переменную с кэшированием.
    /// </summary>
    private Variable? GetVariableCached(string variableName)
    {
        if (_variableCache.TryGetValue(variableName, out var cached))
        {
            return cached;
        }
        
        // Ищем в таблицах
        var variable = _globalTables.FindVariable(variableName);
        
        // Кэшируем результат (даже null)
        _variableCache[variableName] = variable;
        
        return variable;
    }
    
    /// <summary>
    /// Очистить кэш для переменной.
    /// </summary>
    private void InvalidateVariableCache(string variableName)
    {
        _variableCache.TryRemove(variableName, out _);
    }
    
    /// <summary>
    /// Очистить весь кэш переменных.
    /// </summary>
    public void InvalidateAllCache()
    {
        _variableCache.Clear();
        _logger.LogDebug("Variable cache invalidated");
    }
    
    /// <summary>
    /// Получить все привязки.
    /// </summary>
    public IReadOnlyCollection<Binding> GetAllBindings() => _bindings.Values.ToList().AsReadOnly();
    
    public Binding? GetBinding(string sourceName, string targetName)
    {
        var key = GetBindingKey(sourceName, targetName);
        return _bindings.TryGetValue(key, out var binding) ? binding : null;
    }
    
    /// <summary>
    /// Включить/выключить привязку.
    /// </summary>
    public bool SetBindingEnabled(string sourceName, string targetName, bool enabled)
    {
        var key = GetBindingKey(sourceName, targetName);
        if (_bindings.TryGetValue(key, out var binding))
        {
            binding.IsEnabled = enabled;
            _logger.LogDebug("Binding {Key} {Status}", key, enabled ? "enabled" : "disabled");
            return true;
        }
        return false;
    }
    
    private static string GetBindingKey(string source, string target) => $"{source}|{target}";
    
    /// <summary>
    /// Очистить все привязки.
    /// </summary>
    public void Clear()
    {
        _bindings.Clear();
        InvalidateAllCache();
        _logger.LogInformation("All bindings cleared");
    }
}

/// <summary>
/// Интерфейс для сервиса привязок.
/// </summary>
public interface IBindingService
{
    Binding Bind(string sourceName, string targetName, BindingDirection direction = BindingDirection.Bidirectional);
    bool Unbind(string sourceName, string targetName);
    void UpdateValue(string variableName, object? newValue);
    IReadOnlyCollection<Binding> GetAllBindings();
    Binding? GetBinding(string sourceName, string targetName);
    bool SetBindingEnabled(string sourceName, string targetName, bool enabled);
    void Clear();
    void InvalidateAllCache();
}
