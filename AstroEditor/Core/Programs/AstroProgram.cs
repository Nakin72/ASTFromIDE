using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroEditor.Core.Variables;


namespace AstroEditor.Core.Programs;

public class AstroProgram
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("author")] public string Author { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0";
    [JsonPropertyName("created")] public DateTime Created { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("modified")] public DateTime Modified { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("returnTypeId")] public string? ReturnTypeId { get; set; }
    [JsonIgnore] public DataType? ReturnType { get; set; }
    [JsonPropertyName("isMenuFunction")] public bool IsMenuFunction { get; set; }
    [JsonPropertyName("isBackground")] public bool IsBackground { get; set; }
    [JsonPropertyName("taskGroup")] public ulong TaskGroup { get; set; } = 1; // по умолчанию группа 1
    [JsonPropertyName("arguments")] public List<Argument> Arguments { get; set; } = new();
    [JsonPropertyName("localTables")] public VariableTableSet LocalTables { get; set; } = new() { Name = "LocalVariables", IsGlobal = false };
    [JsonPropertyName("lines")] public List<Instruction> Lines { get; set; } = new();
    [JsonPropertyName("labels")] public ConcurrentDictionary<string, int> Labels { get; set; } = new(); // имя метки -> номер строки
    [JsonPropertyName("permissions")] public ProgramPermissions Permissions { get; set; } = new();
    [JsonPropertyName("maxCycles")] public int? MaxCycles { get; set; }

    // ✅ P0-4: Lock для потокобезопасного доступа к изменяемым коллекциям
    private readonly object _linesLock = new();
    private readonly object _argsLock = new();

    public void AddLocalVariable(Variable variable, DataTypeRegistry registry) => LocalTables.AddVariable(variable, registry);
    public Variable? FindLocalVariable(string name) => LocalTables.FindVariable(name);
    
    /// <summary>
    /// Добавить инструкцию потокобезопасно.
    /// </summary>
    public void AddLine(Instruction line)
    {
        lock (_linesLock) // ✅ P0-4
        {
            Lines.Add(line);
        }
    }
    
    /// <summary>
    /// Добавить аргумент потокобезопасно.
    /// </summary>
    public void AddArgument(Argument arg)
    {
        lock (_argsLock) // ✅ P0-4
        {
            Arguments.Add(arg);
        }
    }
    
    /// <summary>
    /// Добавить метку потокобезопасно.
    /// </summary>
    public void AddLabel(string name, int lineNumber)
    {
        Labels[name] = lineNumber;
    }
    
    /// <summary>
    /// Получить инструкцию потокобезопасно.
    /// </summary>
    public Instruction? GetLine(int index)
    {
        lock (_linesLock) // ✅ P0-4
        {
            return index >= 0 && index < Lines.Count ? Lines[index] : null;
        }
    }
}

public class ProgramPermissions
{
    [JsonPropertyName("readRoles")] public List<string> ReadRoles { get; set; } = new() { "operator" };
    [JsonPropertyName("writeRoles")] public List<string> WriteRoles { get; set; } = new() { "programmer" };
    [JsonPropertyName("executeRoles")] public List<string> ExecuteRoles { get; set; } = new() { "operator", "programmer" };
}