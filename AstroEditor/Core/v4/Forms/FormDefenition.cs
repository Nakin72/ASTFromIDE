using System.Text.Json.Serialization;
using AstroEditor.Core.v4.Common;

namespace AstroEditor.Core.v4.Forms;

public class FormDefinition
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("accessLevel")] public FormAccessLevel AccessLevel { get; set; } = FormAccessLevel.User;
    [JsonPropertyName("fields")] public List<FormFieldDefinition> Fields { get; set; } = new();
    [JsonPropertyName("executionProgramId")] public string? ExecutionProgramId { get; set; }
    [JsonPropertyName("insertAction")] public string? InsertAction { get; set; }
    [JsonPropertyName("isControlFlow")] public bool IsControlFlow { get; set; }
    [JsonPropertyName("controlFlow")] public ControlFlowStructure? ControlFlow { get; set; }
}

public class ControlFlowStructure
{
    [JsonPropertyName("openingKeyword")] public string OpeningKeyword { get; set; } = string.Empty;
    [JsonPropertyName("closingKeyword")] public string ClosingKeyword { get; set; } = string.Empty;
    [JsonPropertyName("requiresBody")] public bool RequiresBody { get; set; } = true;
    [JsonPropertyName("canBeNested")] public bool CanBeNested { get; set; } = true;
}