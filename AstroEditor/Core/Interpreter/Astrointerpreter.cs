// AstroEditor.Core/Interpreter/AstroInterpreter.cs
// Ядро интерпретатора - базовые классы и публичные методы
// Обработчики инструкций регистрируются автоматически через атрибуты
// Поддержка плагинов через PluginManager

using System.Diagnostics;
using System.Reflection;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Common;
using AstroEditor.Core.Plugins;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    private readonly InterpreterContext _context;
    private InterpreterState _state = null!;
    private readonly Dictionary<string, Action<Instruction>> _instructionHandlers;
    private readonly ExpressionParser _parser = new();
    private readonly ExpressionEvaluator _evaluator = new();
    private bool _isRunning;
    private readonly PluginManager? _pluginManager;

    public InterpreterState State => _state;
    public InterpreterContext Context => _context;
    public bool IsRunning => _isRunning;
    public PluginManager? PluginManager => _pluginManager;

    public AstroInterpreter(InterpreterContext context, PluginManager? pluginManager = null)
    {
        _context = context;
        _pluginManager = pluginManager;
        _instructionHandlers = InitializeHandlers();
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
            var handler = (Action<Instruction>)Delegate.CreateDelegate(
                typeof(Action<Instruction>), this, method);
            handlers[attr.FormId] = handler;
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
                throw new Exception($"Тип аргумента '{arg.TypeId}' не найден");

            var existing = _state.CurrentLocalTables.FindVariable(arg.Name);
            if (existing == null)
            {
                var variable = new Variable(arg.Name, type, arg.DefaultValue);
                _state.CurrentLocalTables.AddVariable(variable, _context.TypeRegistry);
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

        try
        {
            while (!_state.StopRequested && _state.CurrentLineIndex < _state.Program.Lines.Count)
            {
                if (_state.PauseRequested)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var instruction = _state.Program.Lines[_state.CurrentLineIndex];
                _context.RaiseOnBeforeInstruction(_state, instruction);

                try
                {
                    ExecuteInstruction(instruction);
                }
                catch (Exception ex)
                {
                    _context.RaiseOnError(_state, ex);
                    throw new Exception($"Error at line {instruction.LineNumber}: {ex.Message}", ex);
                }

                _context.RaiseOnAfterInstruction(_state, instruction);

                if (_state.CurrentLineIndex < _state.Program.Lines.Count &&
                    _state.CurrentLineIndex == _state.Program.Lines.IndexOf(instruction))
                {
                    _state.CurrentLineIndex++;
                }
            }
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
        }
        catch (Exception ex)
        {
            _context.RaiseOnError(_state, ex);
            throw;
        }

        _context.RaiseOnAfterInstruction(_state, instruction);
        if (_state.CurrentLineIndex == _state.Program.Lines.IndexOf(instruction))
        {
            _state.CurrentLineIndex++;
        }
    }

    public void Stop() => _state.StopRequested = true;
    public void Pause() => _state.PauseRequested = true;
    public void Resume() => _state.PauseRequested = false;

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
        }
    }

    private void ExecuteInstruction(Instruction instruction)
    {
        if (_instructionHandlers.TryGetValue(instruction.FormId, out var handler))
            handler(instruction);
        else
            throw new NotSupportedException($"Form '{instruction.FormId}' is not supported.");
    }
}
