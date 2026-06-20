// AstroEditor.Core/Plugins/CSharpScriptEngine.cs
// Движок для выполнения C# скриптов без явной компиляции
// Использует Roslyn Scripting API для интерпретации кода

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Plugins;

/// <summary>
/// Движок для выполнения C# скриптов
/// </summary>
public class CSharpScriptEngine
{
    private readonly ScriptOptions _scriptOptions;
    private readonly Dictionary<string, object> _globalVariables = new();
    private ScriptState? _lastState;

    public CSharpScriptEngine()
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        _scriptOptions = ScriptOptions.Default
            .WithReferences(references)
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks",
                "AstroEditor.Core",
                "AstroEditor.Core.Interpreter",
                "AstroEditor.Core.Programs",
                "AstroEditor.Core.Expressions",
                "AstroEditor.Core.Plugins",
                "AstroEditor.Core.Variables",
                "AstroEditor.Core.Tables"
            );
    }

    /// <summary>
    /// Выполнить скрипт и вернуть результат
    /// </summary>
    public async Task<object?> ExecuteScriptAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await CSharpScript.RunAsync(
                code,
                _scriptOptions,
                globals: _globalVariables,
                cancellationToken: cancellationToken
            );

            _lastState = state;
            
            // Обновляем глобальные переменные из состояния скрипта
            foreach (var variable in state.Variables)
            {
                _globalVariables[variable.Name] = variable.Value ?? new object();
            }

            return state.ReturnValue;
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
            throw new Exception($"Script compilation error:\n{errors}", ex);
        }
    }

    /// <summary>
    /// Выполнить скрипт синхронно
    /// </summary>
    public object? ExecuteScript(string code)
    {
        return ExecuteScriptAsync(code).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Выполнить скрипт с продолжением (для цепочки команд)
    /// </summary>
    public async Task<object?> ExecuteContinuationAsync(string code, CancellationToken cancellationToken = default)
    {
        if (_lastState == null)
            return await ExecuteScriptAsync(code, cancellationToken);

        try
        {
            var state = await _lastState.ContinueWithAsync(
                code,
                _scriptOptions,
                cancellationToken: cancellationToken
            );

            _lastState = state;

            foreach (var variable in state.Variables)
            {
                _globalVariables[variable.Name] = variable.Value ?? new object();
            }

            return state.ReturnValue;
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
            throw new Exception($"Script continuation error:\n{errors}", ex);
        }
    }

    /// <summary>
    /// Установить глобальную переменную для скриптов
    /// </summary>
    public void SetGlobalVariable(string name, object value)
    {
        _globalVariables[name] = value;
    }

    /// <summary>
    /// Получить глобальную переменную
    /// </summary>
    public object? GetGlobalVariable(string name)
    {
        return _globalVariables.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Очистить все глобальные переменные и состояние
    /// </summary>
    public void Reset()
    {
        _globalVariables.Clear();
        _lastState = null;
    }

    /// <summary>
    /// Проверить синтаксис скрипта без выполнения
    /// </summary>
    public bool ValidateSyntax(string code, out string[] errors)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics();
        
        errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        return errors.Length == 0;
    }
}

/// <summary>
/// Обёртка для создания инструкций из скриптов
/// </summary>
public class ScriptInstructionHandler
{
    private readonly CSharpScriptEngine _engine;
    private readonly string _handlerCode;

    public ScriptInstructionHandler(CSharpScriptEngine engine, string handlerCode)
    {
        _engine = engine;
        _handlerCode = handlerCode;
    }

    public void Execute(Instruction instruction)
    {
        // Передаём инструкцию в скрипт
        _engine.SetGlobalVariable("instruction", instruction);
        _engine.ExecuteScript(_handlerCode);
    }
}
