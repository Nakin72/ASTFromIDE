// AstroEditor.Core/Interpreter/AstroInterpreterEx.cs
// Расширенная версия интерпретатора с кэшированием и логированием

using System.Diagnostics;
using System.Reflection;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Common;
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Interpreter;

/// <summary>
/// Расширенная версия интерпретатора с поддержкой кэширования AST и логирования.
/// ✅ P2: Реализует IDisposable для освобождения ресурсов.
/// </summary>
public partial class AstroInterpreterEx : IDisposable
{
    private readonly InterpreterContext _context;
    private InterpreterState _state = null!;
    private readonly Dictionary<string, Action<Instruction>> _instructionHandlers;
    private readonly ExpressionParser _parser;
    private readonly ExpressionEvaluator _evaluator;
    private readonly IExpressionCache? _expressionCache;
    private readonly ILogger _logger;
    private bool _isRunning;
    private readonly PluginManager? _pluginManager;

    // ✅ P2: Флаг для IDisposable
    private bool _disposed;

    public InterpreterState State => _state;
    public InterpreterContext Context => _context;
    public bool IsRunning => _isRunning;
    public PluginManager? PluginManager => _pluginManager;
    
    /// <summary>
    /// Статистика выполнения.
    /// </summary>
    public InterpreterStatistics Statistics { get; private set; } = new();

    public AstroInterpreterEx(
        InterpreterContext context, 
        PluginManager? pluginManager = null, 
        IExpressionCache? expressionCache = null, 
        ILogger? logger = null)
    {
        _context = context;
        _pluginManager = pluginManager;
        _expressionCache = expressionCache ?? context.ExpressionCache ?? new ExpressionCache();
        _parser = new ExpressionParser();
        _evaluator = new ExpressionEvaluator();
        _logger = logger ?? Log.For<AstroInterpreterEx>();
        _instructionHandlers = InitializeHandlers();
        
        _logger.LogDebug("AstroInterpreterEx initialized, ExpressionCache={CacheEnabled}, BindingService={BindingEnabled}", 
            _expressionCache != null ? "enabled" : "disabled",
            context.BindingService != null ? "enabled" : "disabled");
    }

    /// <summary>
    /// Парсить выражение с кэшированием (P1-6).
    /// Все partial-классы AstroInterpreterEx используют этот метод.
    /// </summary>
    protected ExpressionNode ParseCachedExpression(string expressionText)
    {
        if (_expressionCache != null)
            return _expressionCache.GetOrParse(expressionText);
        return _parser.Parse(expressionText);
    }

    private Dictionary<string, Action<Instruction>> InitializeHandlers()
    {
        var handlers = new Dictionary<string, Action<Instruction>>();
        
        // 1. Автоматическая регистрация через атрибуты (рефлексия)
        var methods = GetType()
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<InstructionHandlerAttribute>() != null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<InstructionHandlerAttribute>()!;
            try
            {
                var handler = (Action<Instruction>)Delegate.CreateDelegate(
                    typeof(Action<Instruction>), this, method);
                handlers[attr.FormId] = handler;
                _logger.LogTrace("Registered handler for {FormId}", attr.FormId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register handler for {FormId}", attr.FormId);
            }
        }

        // 2. Регистрация внешних обработчиков (для плагинов/расширений)
        RegisterExternalHandlers(handlers);

        return handlers;
    }

    /// <summary>
    /// Переопределите в partial классе для регистрации внешних обработчиков
    /// </summary>
    partial void RegisterExternalHandlers(Dictionary<string, Action<Instruction>> handlers);

    public void LoadProgram(AstroProgram program, VariableTableSet? localTables = null)
    {
        _logger.LogInformation("Loading program {ProgramName} with {LinesCount} lines", 
            program.Name, program.Lines.Count);
        
        _state = new InterpreterState
        {
            Program = program,
            CurrentLocalTables = localTables ?? program.LocalTables,
            CurrentLineIndex = 0,
            CallStack = new Stack<CallFrame>(),
            LoopStack = new Stack<LoopContext>(),
            SwitchStack = new Stack<SwitchContext>(),
            StopRequested = false,
            PauseRequested = false,
            ReturnValue = null
        };

        foreach (var arg in program.Arguments)
        {
            var type = _context.TypeRegistry.GetTypeById(arg.TypeId);
            if (type == null)
            {
                _logger.LogError("Type '{TypeId}' not found for argument '{ArgName}'", 
                    arg.TypeId, arg.Name);
                throw new Exception($"Тип аргумента '{arg.TypeId}' не найден");
            }

            var existing = _state.CurrentLocalTables.FindVariable(arg.Name);
            if (existing == null)
            {
                var variable = new Variable(arg.Name, type, arg.DefaultValue);
                _state.CurrentLocalTables.AddVariable(variable, _context.TypeRegistry);
            }
        }
        
        // Прекомпиляция выражений (если есть кэш)
        if (_expressionCache != null)
        {
            var expressions = ExtractExpressions(program);
            _expressionCache.PreCache(expressions);
            _logger.LogInformation("Pre-cached {Count} expressions", expressions.Count());
        }
    }
    
    private IEnumerable<string> ExtractExpressions(AstroProgram program)
    {
        foreach (var instruction in program.Lines)
        {
            foreach (var field in instruction.Fields.Values)
            {
                if (field is ExpressionFieldValue exprField)
                    yield return exprField.Expression;
            }
        }
    }

    public void Run()
    {
        if (_state == null)
            throw new InvalidOperationException("Program not loaded. Call LoadProgram first.");

        _isRunning = true;
        _state.StopRequested = false;
        _state.PauseRequested = false;
        Statistics = new InterpreterStatistics { StartTime = DateTime.UtcNow };

        try
        {
            while (!_state.StopRequested && _state.CurrentLineIndex < _state.Program.Lines.Count)
            {
                if (_state.PauseRequested)
                {
                    // ✅ P2: Task.Delay вместо Thread.Sleep
                    Task.Delay(10).Wait(_state.StopRequested ? CancellationToken.None : default);
                    continue;
                }

                var instruction = _state.Program.Lines[_state.CurrentLineIndex];
                _context.RaiseOnBeforeInstruction(_state, instruction);

                try
                {
                    ExecuteInstruction(instruction);
                    Statistics.InstructionsExecuted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing instruction at line {LineNumber} (FormId: {FormId})", 
                        instruction.LineNumber, instruction.FormId);
                    _context.RaiseOnError(_state, ex);
                    throw new Exception($"Error at line {instruction.LineNumber}: {ex.Message}", ex);
                }

                _context.RaiseOnAfterInstruction(_state, instruction);

                _state.CurrentLineIndex++;
            }
            
            Statistics.EndTime = DateTime.UtcNow;
            _logger.LogInformation("Program completed. Instructions: {Count}, Duration: {Duration}", 
                Statistics.InstructionsExecuted, Statistics.Duration);
        }
        catch (Exception ex)
        {
            Statistics.EndTime = DateTime.UtcNow;
            Statistics.Exception = ex;
            _logger.LogError(ex, "Program execution failed");
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Step()
    {
        if (_state == null || _state.StopRequested || _state.CurrentLineIndex >= _state.Program.Lines.Count)
            return;

        var instruction = _state.Program.Lines[_state.CurrentLineIndex];
        _context.RaiseOnBeforeInstruction(_state, instruction);

        try
        {
            ExecuteInstruction(instruction);
            Statistics.InstructionsExecuted++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stepping instruction at line {LineNumber}", instruction.LineNumber);
            _context.RaiseOnError(_state, ex);
            throw;
        }

        _context.RaiseOnAfterInstruction(_state, instruction);
        _state.CurrentLineIndex++;
    }

    public void Stop()
    {
        if (_state != null)
        {
            _state.StopRequested = true;
            _logger.LogInformation("Stop requested");
        }
    }
    
    public void Pause()
    {
        _state.PauseRequested = true;
        _logger.LogDebug("Pause requested");
    }
    
    public void Resume()
    {
        _state.PauseRequested = false;
        _logger.LogDebug("Resume requested");
    }
    
    public void Reset()
    {
        if (_state != null)
        {
            _state.CurrentLineIndex = 0;
            _state.CallStack.Clear();
            _state.LoopStack.Clear();
            _state.SwitchStack.Clear();
            _state.StopRequested = false;
            _state.PauseRequested = false;
            _state.ReturnValue = null;
            Statistics = new InterpreterStatistics();
            _logger.LogDebug("Interpreter reset");
        }
    }

    private void ExecuteInstruction(Instruction instruction)
    {
        _logger.LogTrace("Executing instruction {LineNumber}: {FormId}", 
            instruction.LineNumber, instruction.FormId);
        
        if (_instructionHandlers.TryGetValue(instruction.FormId, out var handler))
        {
            try
            {
                handler(instruction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler failed for instruction {FormId} at line {LineNumber}", 
                    instruction.FormId, instruction.LineNumber);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("No handler for form {FormId} at line {LineNumber}", 
                instruction.FormId, instruction.LineNumber);
            throw new NotSupportedException($"Form '{instruction.FormId}' is not supported.");
        }
    }
    
    /// <summary>
    /// Получить кэш выражений (для внешнего использования).
    /// </summary>
    public IExpressionCache? GetExpressionCache() => _expressionCache;
    
    /// <summary>
    /// Получить статистику выполнения.
    /// </summary>
    public InterpreterStatistics GetStatistics() => Statistics;
    
    #region IDisposable
    
    /// <summary>
    /// Освободить ресурсы интерпретатора.
    /// ✅ P2: Реализация IDisposable паттерна.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Освободить управляемые и неуправляемые ресурсы.
    /// </summary>
    /// <param name="disposing">true для освобождения управляемых ресурсов</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            // ✅ Освобождаем управляемые ресурсы
            Stop();
            Reset();
            
            // ✅ P2: Очищаем кэш выражений (если он IDisposable)
            if (_expressionCache is IDisposable cacheDisposable)
            {
                cacheDisposable.Dispose();
            }
            
            _logger.LogDebug("Interpreter disposed");
        }
        
        // ✅ Здесь можно освободить неуправляемые ресурсы (если будут)
        
        _disposed = true;
    }
    
    /// <summary>
    /// Финализатор для гарантированного освобождения ресурсов.
    /// </summary>
    ~AstroInterpreterEx()
    {
        Dispose(false);
    }
    
    /// <summary>
    /// Проверить, не disposed ли объект.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AstroInterpreterEx));
    }
    
    #endregion
}

/// <summary>
/// Статистика выполнения интерпретатора.
/// </summary>
public class InterpreterStatistics
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long InstructionsExecuted { get; set; }
    public Exception? Exception { get; set; }
    
    public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;
    
    public double InstructionsPerSecond => 
        Duration.TotalSeconds > 0 ? InstructionsExecuted / Duration.TotalSeconds : 0;
    
    public override string ToString() => 
        $"Instructions: {InstructionsExecuted}, Duration: {Duration}, Speed: {InstructionsPerSecond:F0} instr/s";
}
