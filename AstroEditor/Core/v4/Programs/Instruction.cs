using System.Text.Json.Serialization;

namespace AstroEditor.Core.v4.Programs;

public class Instruction
{
    [JsonPropertyName("lineNumber")] public int LineNumber { get; set; }
    [JsonPropertyName("comment")] public string? Comment { get; set; }
    [JsonPropertyName("formId")] public string FormId { get; set; } = string.Empty;
    [JsonPropertyName("fields")] public Dictionary<string, FieldValue> Fields { get; set; } = new();
    [JsonPropertyName("breakpoint")] public bool Breakpoint { get; set; }

    public Instruction() { }
    public Instruction(int lineNumber, string formId) { LineNumber = lineNumber; FormId = formId; }
}