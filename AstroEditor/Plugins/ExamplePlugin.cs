// AstroEditor/Plugins/ExamplePlugin.cs
// Пример плагина для демонстрации системы расширений
// Компилируется в отдельную .dll и загружается из папки Plugins/

using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;

namespace AstroEditor.Plugins;

[Plugin("ExamplePlugin", "1.0.0", "Пример плагина для демонстрации системы расширений", Author = "AstroEditor Team")]
public class ExamplePlugin : IPlugin
{
    public string Name => "ExamplePlugin";
    public string Version => "1.0.0";
    public string Description => "Пример плагина для демонстрации системы расширений";

    private PluginContext? _context;

    public void OnLoad(PluginContext context)
    {
        _context = context;

        context.Log("Info", "ExamplePlugin loading...");

        // 1. Регистрируем новую инструкцию
        context.RegisterInstruction("plugin.example.hello", ExecuteHello);

        // 2. Регистрируем новую функцию
        context.RegisterFunction("PLUGIN_UPPER", new UpperFunction());

        context.Log("Info", "ExamplePlugin loaded successfully!");
    }

    public void OnUnload()
    {
        _context?.Log("Info", "ExamplePlugin unloaded");
    }

    // Обработчик новой инструкции
    private void ExecuteHello(Instruction instruction)
    {
        var messageField = GetFieldValue(instruction, "message");
        _context?.Log("Info", $"[HELLO] {messageField}");
        Console.WriteLine($">>> HELLO FROM PLUGIN: {messageField}");
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

// Пример встроенной функции от плагина
public class UpperFunction : IBuiltinFunction
{
    public object? Execute(params object?[] args)
    {
        if (args.Length == 0 || args[0] == null)
            return null;

        var input = args[0].ToString();
        return input?.ToUpper();
    }

    public int RequiredArgCount => 1;
    public bool HasVariableArgs => false;
}
