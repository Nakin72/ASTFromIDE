using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Tables;
using AstroEditor.Core.v4.Types;
using AstroEditor.Core.v4.Variables;


namespace AstroEditor.Core.v4.Programs;

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
    [JsonPropertyName("labels")] public Dictionary<string, int> Labels { get; set; } = new(); // имя метки -> номер строки
    [JsonPropertyName("permissions")] public ProgramPermissions Permissions { get; set; } = new();
    [JsonPropertyName("maxCycles")] public int? MaxCycles { get; set; }

    public void AddLocalVariable(Variable variable, DataTypeRegistry registry) => LocalTables.AddVariable(variable, registry);
    public Variable? FindLocalVariable(string name) => LocalTables.FindVariable(name);
}

public class ProgramPermissions
{
    [JsonPropertyName("readRoles")] public List<string> ReadRoles { get; set; } = new() { "operator" };
    [JsonPropertyName("writeRoles")] public List<string> WriteRoles { get; set; } = new() { "programmer" };
    [JsonPropertyName("executeRoles")] public List<string> ExecuteRoles { get; set; } = new() { "operator", "programmer" };
}