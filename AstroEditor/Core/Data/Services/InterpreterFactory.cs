// AstroEditor.Core/Data/Services/InterpreterFactory.cs
// Фабрика интерпретаторов — вынесена из ProjectManager (P1-8 SRP)

using AstroEditor.Core.Common.Logging;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using Microsoft.Extensions.Logging;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Реализация фабрики интерпретаторов.
/// Инкапсулирует создание InterpreterContext, ExpressionCache и AstroInterpreterEx.
/// </summary>
public class InterpreterFactory : IInterpreterFactory
{
    private readonly DataTypeRegistry _typeRegistry;
    private readonly FormRegistry _formRegistry;
    private readonly VariableTableSet _globalTables;
    private readonly Dictionary<string, AstroProgram> _programRegistry;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private PluginManager? _pluginManager;

    public InterpreterFactory(
        DataTypeRegistry typeRegistry,
        FormRegistry formRegistry,
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programRegistry,
        Dictionary<string, Func<object?[], object?>> functions,
        PluginManager? pluginManager = null)
    {
        _typeRegistry = typeRegistry;
        _formRegistry = formRegistry;
        _globalTables = globalTables;
        _programRegistry = programRegistry;
        _functions = functions;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Обновить PluginManager (вызывается после InitializeNew).
    /// </summary>
    public void UpdatePluginManager(PluginManager? pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public AstroInterpreterEx CreateInterpreter()
    {
        var logger = Log.For<AstroInterpreterEx>();
        
        // Создаём контекст интерпретатора
        var context = new InterpreterContext
        {
            TypeRegistry = _typeRegistry,
            FormRegistry = _formRegistry,
            GlobalTables = _globalTables,
            Functions = _functions,
            ProgramRegistry = _programRegistry
        };
        
        // Создаём кэш выражений
        var expressionCache = new ExpressionCache();
        
        return new AstroInterpreterEx(
            context, 
            _pluginManager,
            expressionCache,
            logger);
    }
}
