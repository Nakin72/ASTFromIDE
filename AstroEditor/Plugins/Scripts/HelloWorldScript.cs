// AstroEditor/Plugins/Scripts/HelloWorldScript.cs
// Пример скрипт-плагина — компилируется во время выполнения
// Не требует предварительной компиляции в .dll

using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;

namespace AstroEditor.Plugins.Scripts;

[Plugin("HelloWorldScript", "1.0.0", "Пример скрипт-плагина для демонстрации runtime компиляции", Author = "AstroEditor Team")]
public class HelloWorldScript : IPlugin
{
    public string Name => "HelloWorldScript";
    public string Version => "1.0.0";
    public string Description => "Пример скрипт-плагина для демонстрации runtime компиляции";

    private PluginContext? _context;

    public void OnLoad(PluginContext context)
    {
        _context = context;
        context.Log("Info", "HelloWorldScript loading from SOURCE CODE (not DLL)!");

        // Регистрируем инструкцию
        context.RegisterInstruction("script.hello", ExecuteHello);
        
        // Регистрируем функцию
        context.RegisterFunction("SCRIPT_LOWER", new LowerFunction());

        context.Log("Info", "HelloWorldScript loaded successfully!");
    }

    public void OnUnload()
    {
        _context?.Log("Info", "HelloWorldScript unloaded");
    }

    private void ExecuteHello(Instruction instruction)
    {
        var messageField = GetFieldValue(instruction, "message");
        _context?.Log("Info", $"[SCRIPT HELLO] {messageField}");
        Console.WriteLine($">>> SCRIPT PLUGIN SAYS: {messageField}");
    }

    private string GetFieldValue(Instruction instruction, string fieldName)
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && field is ConstantFieldValue constField)
        {
            return constField.Value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }
}

// Встроенная функция из скрипта
public class LowerFunction : IBuiltinFunction
{
    public object? Execute(params object?[] args)
    {
        if (args.Length == 0 || args[0] == null)
            return null;

        var input = args[0].ToString();
        return input?.ToLower();
    }

    public int RequiredArgCount => 1;
    public bool HasVariableArgs => false;
}
