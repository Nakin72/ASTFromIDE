// AstroEditor.Core/Plugins/ScriptPluginLoader.cs
// Загрузчик скрипт-плагинов с компиляцией во время выполнения
// Использует Roslyn для компиляции C# кода в runtime

using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Plugins;

/// <summary>
/// Загрузчик плагинов из исходного кода C# с компиляцией в runtime
/// </summary>
public class ScriptPluginLoader
{
    private readonly string _scriptsFolder;
    private readonly string _cacheFolder;
    private readonly PluginManager _pluginManager;
    private readonly List<(string ScriptPath, Assembly Assembly)> _compiledAssemblies = new();

    private readonly Action<string, Action<Instruction>> _registerInstruction;
    private readonly Action<string, IBuiltinFunction> _registerFunction;
    private readonly Action<FormDefinition> _registerForm;
    private readonly Action<DataType> _registerType;

    public ScriptPluginLoader(
        string scriptsFolder,
        string cacheFolder,
        PluginManager pluginManager,
        Action<string, Action<Instruction>> registerInstruction,
        Action<string, IBuiltinFunction> registerFunction,
        Action<FormDefinition> registerForm,
        Action<DataType> registerType)
    {
        _scriptsFolder = scriptsFolder;
        _cacheFolder = cacheFolder;
        _pluginManager = pluginManager;
        _registerInstruction = registerInstruction;
        _registerFunction = registerFunction;
        _registerForm = registerForm;
        _registerType = registerType;

        Directory.CreateDirectory(_scriptsFolder);
        Directory.CreateDirectory(_cacheFolder);
    }

    /// <summary>
    /// Загрузить и скомпилировать все скрипты из папки
    /// </summary>
    public void LoadAllScripts()
    {
        if (!Directory.Exists(_scriptsFolder))
            return;

        var scriptFiles = Directory.GetFiles(_scriptsFolder, "*.cs", SearchOption.AllDirectories);

        foreach (var scriptFile in scriptFiles)
        {
            try
            {
                CompileAndLoadScript(scriptFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptLoader] ERROR compiling {Path.GetFileName(scriptFile)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Загрузить и скомпилировать отдельный скрипт
    /// </summary>
    public void CompileAndLoadScript(string scriptPath)
    {
        var sourceCode = File.ReadAllText(scriptPath);
        var assembly = CompileSource(sourceCode, scriptPath);

        if (assembly != null)
        {
            _compiledAssemblies.Add((scriptPath, assembly));
            LoadPluginsFromAssembly(assembly, scriptPath);
            Console.WriteLine($"[ScriptLoader] COMPILED & LOADED: {Path.GetFileName(scriptPath)}");
        }
    }

    /// <summary>
    /// Перекомпилировать скрипт (для горячей перезагрузки)
    /// </summary>
    public void ReloadScript(string scriptPath)
    {
        var existing = _compiledAssemblies.FirstOrDefault(x => x.ScriptPath == scriptPath);
        if (existing.Assembly != null)
        {
            // Выгружаем старые плагины из этой сборки
            var pluginsToRemove = _pluginManager.LoadedPlugins
                .Where(p => p.Value.GetType().Assembly == existing.Assembly)
                .Select(p => p.Key)
                .ToList();

            foreach (var pluginName in pluginsToRemove)
            {
                _pluginManager.UnloadPlugin(pluginName);
            }

            _compiledAssemblies.RemoveAll(x => x.ScriptPath == scriptPath);
        }

        CompileAndLoadScript(scriptPath);
    }

    /// <summary>
    /// Компиляция исходного кода в сборку
    /// </summary>
    private Assembly? CompileSource(string sourceCode, string scriptPath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
            
            // Ссылки на AstroEditor
            MetadataReference.CreateFromFile(typeof(IPlugin).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AstroInterpreterEx).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IBuiltinFunction).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(FormDefinition).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DataType).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Instruction).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            $"ScriptPlugin_{Path.GetFileNameWithoutExtension(scriptPath)}_{Guid.NewGuid():N8}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = new StringBuilder();
            errors.AppendLine($"Compilation failed for {scriptPath}:");
            foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                errors.AppendLine($"  {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
            throw new Exception(errors.ToString());
        }

        ms.Position = 0;

        // Кэшируем DLL на диск
        var cachePath = Path.Combine(_cacheFolder, Path.GetFileNameWithoutExtension(scriptPath) + ".dll");
        using (var fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
        {
            ms.CopyTo(fs);
        }

        // Загружаем сборку
        var assembly = Assembly.Load(ms.ToArray());
        return assembly;
    }

    /// <summary>
    /// Загрузка плагинов из скомпилированной сборки
    /// </summary>
    private void LoadPluginsFromAssembly(Assembly assembly, string scriptPath)
    {
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            var attr = pluginType.GetCustomAttribute<PluginAttribute>();
            if (attr == null)
            {
                Console.WriteLine($"[ScriptLoader] WARNING: {pluginType.Name} has no [Plugin] attribute");
                continue;
            }

            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                var context = new PluginContext(
                    _registerInstruction,
                    _registerFunction,
                    _registerForm,
                    _registerType,
                    (level, msg) => Console.WriteLine($"[{attr.Name}] {level}: {msg}")
                );

                plugin.OnLoad(context);
                // Плагины из скриптов регистрируем через PluginManager
                // Но не добавляем в _loadedPlugins чтобы избежать конфликтов
                Console.WriteLine($"[ScriptLoader] LOADED SCRIPT PLUGIN: {attr.Name} v{attr.Version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptLoader] ERROR initializing {attr.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Получить список загруженных скриптов
    /// </summary>
    public IReadOnlyList<string> GetLoadedScripts()
    {
        return _compiledAssemblies.Select(x => x.ScriptPath).ToList().AsReadOnly();
    }
}
