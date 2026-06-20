// AstroEditor.Core.v4/Serialization/ProgramTextGenerator.cs
using System.Text;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Common;
namespace AstroEditor.Core.Serialization;

public static class ProgramTextGenerator
{
    private const string Indent = "  ";
    private const string Indent2 = "    ";
    private static readonly string Separator = new string('─', 80);
    public static string Generate(AstroProgram program, bool detailed = false)
    {
        var sb = new StringBuilder();

        // Заголовок
        sb.AppendLine(Separator);
        sb.AppendLine($"PROGRAM: {program.Name}");
        sb.AppendLine($"AUTHOR: {program.Author}");
        sb.AppendLine($"VERSION: {program.Version}");
        sb.AppendLine($"TYPE: {(program.ReturnTypeId != null ? "FUNCTION" : "PROCEDURE")}");
        if (program.IsMenuFunction) sb.AppendLine("  (MENU FUNCTION)");
        if (program.IsBackground) sb.AppendLine("  (BACKGROUND PROCESS)");
        sb.AppendLine($"TASK GROUP: {program.TaskGroup}");
        if (program.MaxCycles.HasValue)
            sb.AppendLine($"MAX CYCLES: {program.MaxCycles}");
        if (!string.IsNullOrEmpty(program.Description))
            sb.AppendLine($"DESCRIPTION: {program.Description}");
        sb.AppendLine(Separator);

        // Аргументы
        if (program.Arguments.Any())
        {
            sb.AppendLine("\nARGUMENTS:");
            foreach (var arg in program.Arguments)
            {
                var defaultStr = arg.DefaultValue != null ? $" = {FormatValue(arg.DefaultValue)}" : "";
                var dirStr = arg.Direction != ArgumentDirection.In ? $" ({arg.Direction})" : "";
                sb.AppendLine($"{Indent}{arg.Name} : {arg.TypeId}{dirStr}{defaultStr}");
            }
        }

        // Локальные переменные
        if (program.LocalTables.Tables.Any())
        {
            sb.AppendLine("\nLOCAL VARIABLES:");
            foreach (var kv in program.LocalTables.Tables)
            {
                var typeName = kv.Value.Type?.Name ?? kv.Key;
                sb.AppendLine($"{Indent}TYPE: {typeName}");
                if (!kv.Value.Variables.Any())
                {
                    sb.AppendLine($"{Indent2}<empty>");
                }
                else
                {
                    foreach (var v in kv.Value.Variables)
                    {
                        var valueStr = FormatValue(v.Value);
                        var comment = !string.IsNullOrEmpty(v.Comment) ? $" // {v.Comment}" : "";
                        sb.AppendLine($"{Indent2}{v.Name} = {valueStr}{comment}");
                    }
                }
            }
        }

        // Метки
        if (program.Labels.Any())
        {
            sb.AppendLine("\nLABELS:");
            foreach (var lbl in program.Labels)
                sb.AppendLine($"{Indent}{lbl.Key} -> line {lbl.Value}");
        }

        // Инструкции
        sb.AppendLine("\n" + Separator);
        sb.AppendLine("CODE:");
        sb.AppendLine(Separator);

        int indentLevel = 0;
        var controlFlowStack = new Stack<string>();

        foreach (var instr in program.Lines)
        {
            // Управление отступами
            var formId = instr.FormId;
            if (formId == "core.endif" || formId == "core.endwhile" || formId == "core.endfor" || formId == "core.endswitch")
            {
                indentLevel = Math.Max(0, indentLevel - 1);
                if (controlFlowStack.Count > 0 && IsClosingFor(formId, controlFlowStack.Peek()))
                    controlFlowStack.Pop();
            }

            var indent = new string(' ', indentLevel * 4);

            // Строка с номером
            var lineStr = $"{instr.LineNumber,3}";
            var breakpoint = instr.Breakpoint ? "* " : "  ";
            var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  // {instr.Comment}" : "";

            // Отображение формы
            string display;
            switch (formId)
            {
                case "core.assign":
                    var varField = GetFieldValue(instr, "variable");
                    var exprField = GetFieldValue(instr, "expression");
                    display = $"{GetDisplayName(formId)} {varField} = {exprField}";
                    break;
                case "core.while":
                    var condWhile = GetFieldValue(instr, "condition");
                    var maxIter = GetFieldValue(instr, "maxIterations");
                    display = $"{GetDisplayName(formId)} {condWhile}";
                    if (!string.IsNullOrEmpty(maxIter) && maxIter != "1000")
                        display += $" (max {maxIter})";
                    break;
                case "core.for":
                    var forVar = GetFieldValue(instr, "variable");
                    var start = GetFieldValue(instr, "start");
                    var end = GetFieldValue(instr, "end");
                    var step = GetFieldValue(instr, "step");
                    display = $"{GetDisplayName(formId)} {forVar} = {start} TO {end}";
                    if (!string.IsNullOrEmpty(step) && step != "1")
                        display += $" STEP {step}";
                    break;
                case "core.if":
                    var condIf = GetFieldValue(instr, "condition");
                    display = $"{GetDisplayName(formId)} {condIf}";
                    break;
                case "core.switch":
                    var switchExpr = GetFieldValue(instr, "expression");
                    display = $"{GetDisplayName(formId)} {switchExpr}";
                    break;
                case "core.case":
                    var caseVal = GetFieldValue(instr, "value");
                    display = $"{GetDisplayName(formId)} {caseVal}";
                    break;
                case "core.jumpif":
                    var jumpCond = GetFieldValue(instr, "condition");
                    var jumpLabel = GetFieldValue(instr, "labelName");
                    display = $"{GetDisplayName(formId)} {jumpCond} -> {jumpLabel}";
                    break;
                case "core.jumplbl":
                    var label = GetFieldValue(instr, "labelName");
                    display = $"{GetDisplayName(formId)} -> {label}";
                    break;
                case "core.lbl":
                    var lblName = GetFieldValue(instr, "labelName");
                    display = $"{lblName}:";
                    break;
                case "core.call":
                    var progName = GetFieldValue(instr, "programName");
                    var args = GetFieldValue(instr, "arguments", detailed);
                    display = $"{GetDisplayName(formId)} {progName} ({args})";
                    break;
                case "core.return":
                    var retVal = GetFieldValue(instr, "value", detailed);
                    display = $"{GetDisplayName(formId)} {retVal}";
                    break;
                case "core.break":
                    display = GetDisplayName(formId);
                    break;
                case "core.continue":
                    display = GetDisplayName(formId);
                    break;
                default:
                    display = formId.Replace("core.", "");
                    break;
            }

            sb.AppendLine($"{indent}{lineStr} {breakpoint}{display}{comment}");

            // Обновляем отступы для вложенных конструкций
            if (formId == "core.if" || formId == "core.while" || formId == "core.for" || formId == "core.switch")
            {
                indentLevel++;
                controlFlowStack.Push(formId);
            }
            else if (formId == "core.else")
            {
                // Для else отступ остаётся на уровне if
                indentLevel = Math.Max(0, indentLevel - 1);
            }
            else if (formId == "core.case" || formId == "core.default")
            {
                // Для case/default внутри switch используем специальный отступ
            }
        }

        sb.AppendLine(Separator);

        // Права
        sb.AppendLine($"\nPERMISSIONS: R={string.Join(",", program.Permissions.ReadRoles)} " +
                      $"W={string.Join(",", program.Permissions.WriteRoles)} " +
                      $"X={string.Join(",", program.Permissions.ExecuteRoles)}");

        return sb.ToString();
    }
    public static void SaveToFile(AstroProgram program, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, Generate(program), Encoding.UTF8);
    }
    private static string GetDisplayName(string formId)
    {
        return formId.Replace("core.", "").ToUpperInvariant();
    }

    private static string GetFieldValue(Instruction instr, string fieldName, bool detailed = false)
    {
        if (!instr.Fields.TryGetValue(fieldName, out var field))
            return "";

        return field switch
        {
            ConstantFieldValue c => FormatValue(c.Value),
            VariableFieldValue v => $"{v.TableSetName}.{v.VariableName}",
            ExpressionFieldValue e => detailed ? $"\"{e.Expression}\"" : e.Expression,
            EnumFieldValue e => e.SelectedValue,
            FunctionCallFieldValue f => $"{f.FunctionName}({string.Join(",", f.Arguments.Select(a => FormatValue(a)))})",
            LabelFieldValue l => l.LabelName,
            _ => field.ToString() ?? ""
        };
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{s}\"";
        if (value is bool b) return b ? "TRUE" : "FALSE";
        if (value is System.Collections.IEnumerable list && value is not string)
        {
            var items = list.Cast<object>().Select(FormatValue);
            return $"[{string.Join(", ", items)}]";
        }
        return value.ToString() ?? "null";
    }

    private static bool IsClosingFor(string formId, string openFormId)
    {
        return (formId == "core.endif" && openFormId == "core.if") ||
               (formId == "core.endwhile" && openFormId == "core.while") ||
               (formId == "core.endfor" && openFormId == "core.for") ||
               (formId == "core.endswitch" && openFormId == "core.switch");
    }
}