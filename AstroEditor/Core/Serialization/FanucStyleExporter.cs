// AstroEditor.Core.v4/Serialization/FanucStyleExporter.cs
// Экспорт программ в стиле FANUC TP .ls (паскалеподобный формат)
using System.Text;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Common;
using AstroEditor.Core.Types;

namespace AstroEditor.Core.Serialization;

public static class FanucStyleExporter
{
    private const string Indent = "  ";
    
    /// <summary>
    /// Генерирует текст программы в стиле FANUC TP .ls
    /// </summary>
    public static string Generate(AstroProgram program, DataTypeRegistry? typeRegistry = null)
    {
        var sb = new StringBuilder();

        // Заголовок программы в стиле FANUC
        sb.AppendLine("/PROG");
        sb.AppendLine($"  NAME: {program.Name}");
        sb.AppendLine($"  COMMENT: \"{EscapeComment(program.Description)}\"");
        sb.AppendLine($"  VERSION: {program.Version}");
        sb.AppendLine($"  AUTHOR: {program.Author}");
        if (program.ReturnTypeId != null)
        {
            var returnType = typeRegistry?.GetTypeById(program.ReturnTypeId)?.Name ?? program.ReturnTypeId;
            sb.AppendLine($"  RETURN_TYPE: {returnType}");
        }
        if (program.IsBackground)
            sb.AppendLine("  TYPE: BACKGROUND");
        if (program.MaxCycles.HasValue)
            sb.AppendLine($"  MAX_CYCLES: {program.MaxCycles}");
        sb.AppendLine("/ATTR");
        
        // Аргументы
        if (program.Arguments.Any())
        {
            sb.AppendLine();
            sb.AppendLine("!--- ARGUMENTS ---");
            foreach (var arg in program.Arguments)
            {
                var argType = typeRegistry?.GetTypeById(arg.TypeId)?.Name ?? arg.TypeId;
                var dirStr = arg.Direction switch
                {
                    ArgumentDirection.Out => " OUT",
                    ArgumentDirection.Ref => " REF",
                    _ => ""
                };
                var defaultStr = arg.DefaultValue != null ? $" = {FormatValue(arg.DefaultValue)}" : "";
                sb.AppendLine($"  {arg.Name} : {argType}{dirStr}{defaultStr};");
            }
        }

        // Локальные переменные
        if (program.LocalTables.Tables.Any())
        {
            sb.AppendLine();
            sb.AppendLine("!--- LOCAL VARIABLES ---");
            foreach (var kv in program.LocalTables.Tables)
            {
                var typeName = kv.Value.Type?.Name ?? kv.Key;
                foreach (var v in kv.Value.Variables)
                {
                    var valueStr = FormatValue(v.Value);
                    var comment = !string.IsNullOrEmpty(v.Comment) ? $" ! {v.Comment}" : "";
                    sb.AppendLine($"  {v.Name} : {typeName} = {valueStr};{comment}");
                }
            }
        }

        // Тело программы
        sb.AppendLine();
        sb.AppendLine("!--- CODE ---");
        sb.AppendLine("/BODY");

        int indentLevel = 0;
        var controlFlowStack = new Stack<string>();
        var switchValueStack = new Stack<string>();

        foreach (var instr in program.Lines)
        {
            var formId = instr.FormId;
            var indent = new string(' ', indentLevel * 2);
            var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";

            // Обработка закрывающих конструкций
            if (IsClosingInstruction(formId, out var openFormId))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
                if (controlFlowStack.Count > 0 && controlFlowStack.Peek() == openFormId)
                    controlFlowStack.Pop();
                
                if (formId == "core.endswitch" && switchValueStack.Count > 0)
                    switchValueStack.Pop();
            }

            string line;

            switch (formId)
            {
                case "core.assign":
                    line = FormatAssign(instr, indent);
                    break;
                    
                case "core.if":
                    line = FormatIf(instr, indent, controlFlowStack);
                    break;
                    
                case "core.else":
                    line = $"{indent}ELSE{comment}";
                    break;
                    
                case "core.endif":
                    line = $"{indent}END_IF;{comment}";
                    break;
                    
                case "core.while":
                    line = FormatWhile(instr, indent, controlFlowStack);
                    break;
                    
                case "core.endwhile":
                    line = $"{indent}END_WHILE;{comment}";
                    break;
                    
                case "core.for":
                    line = FormatFor(instr, indent, controlFlowStack);
                    break;
                    
                case "core.endfor":
                    line = $"{indent}END_FOR;{comment}";
                    break;
                    
                case "core.foreach":
                    line = FormatForEach(instr, indent, controlFlowStack);
                    break;
                    
                case "core.endforeach":
                    line = $"{indent}END_FOR_EACH;{comment}";
                    break;
                    
                case "core.switch":
                    line = FormatSwitch(instr, indent, controlFlowStack, switchValueStack);
                    break;
                    
                case "core.case":
                    line = FormatCase(instr, indent);
                    break;
                    
                case "core.default":
                    line = $"{indent}DEFAULT:{comment}";
                    break;
                    
                case "core.endswitch":
                    line = $"{indent}END_SWITCH;{comment}";
                    break;
                    
                case "core.lbl":
                    line = FormatLabel(instr, indent);
                    break;
                    
                case "core.jumplbl":
                    line = FormatJumpLbl(instr, indent);
                    break;
                    
                case "core.jumpif":
                    line = FormatJumpIf(instr, indent);
                    break;
                    
                case "core.call":
                    line = FormatCall(instr, indent);
                    break;
                    
                case "core.return":
                    line = FormatReturn(instr, indent);
                    break;
                    
                case "core.break":
                    line = $"{indent}BREAK;{comment}";
                    break;
                    
                case "core.continue":
                    line = $"{indent}CONTINUE;{comment}";
                    break;
                    
                case "core.wait":
                    line = FormatWait(instr, indent);
                    break;
                    
                case "core.nop":
                    line = $"{indent}NOP;{comment}";
                    break;
                    
                case "core.throw":
                    line = FormatThrow(instr, indent);
                    break;
                    
                case "core.try":
                    line = $"{indent}TRY{comment}";
                    indentLevel++;
                    controlFlowStack.Push(formId);
                    continue;
                    
                case "core.catch":
                    indentLevel = Math.Max(0, indentLevel - 1);
                    line = FormatCatch(instr, indent);
                    break;
                    
                case "core.finally":
                    indentLevel = Math.Max(0, indentLevel - 1);
                    line = $"{indent}FINALLY{comment}";
                    indentLevel++;
                    continue;
                    
                case "core.endtry":
                    indentLevel = Math.Max(0, indentLevel - 1);
                    line = $"{indent}END_TRY;{comment}";
                    break;
                    
                case "core.alarm_raise":
                    line = FormatAlarmRaise(instr, indent);
                    break;
                    
                case "core.alarm_clear":
                    line = FormatAlarmClear(instr, indent);
                    break;
                    
                case "core.alarm_ack":
                    line = FormatAlarmAck(instr, indent);
                    break;
                    
                default:
                    line = $"{indent}{formId.Replace("core.", "").ToUpper()};{comment}";
                    break;
            }

            sb.AppendLine(line);

            // Обработка открывающих конструкций (уже обработаны в switch)
            if (formId == "core.if" || formId == "core.while" || 
                formId == "core.for" || formId == "core.foreach" || 
                formId == "core.switch" || formId == "core.try")
            {
                indentLevel++;
                controlFlowStack.Push(formId);
            }
        }

        sb.AppendLine("/END");
        return sb.ToString();
    }

    /// <summary>
    /// Сохраняет программу в файл .ls
    /// </summary>
    public static void SaveToFile(AstroProgram program, string filePath, DataTypeRegistry? typeRegistry = null)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        var content = Generate(program, typeRegistry);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    #region Форматирование инструкций

    private static string FormatAssign(Instruction instr, string indent)
    {
        var varName = GetVariableName(instr, "variable");
        var expr = GetExpression(instr, "expression");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}{varName} := {expr};{comment}";
    }

    private static string FormatIf(Instruction instr, string indent, Stack<string> stack)
    {
        var condition = GetExpression(instr, "condition");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}IF {condition} THEN{comment}";
    }

    private static string FormatWhile(Instruction instr, string indent, Stack<string> stack)
    {
        var condition = GetExpression(instr, "condition");
        var maxIter = GetFieldValue(instr, "maxIterations");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        var maxStr = !string.IsNullOrEmpty(maxIter) && maxIter != "1000" ? $" MAX_ITERATIONS={maxIter}" : "";
        return $"{indent}WHILE {condition} DO{maxStr}{comment}";
    }

    private static string FormatFor(Instruction instr, string indent, Stack<string> stack)
    {
        var varName = GetVariableName(instr, "variable");
        var start = GetExpression(instr, "start");
        var end = GetExpression(instr, "end");
        var step = GetExpression(instr, "step");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        var stepStr = !string.IsNullOrEmpty(step) && step != "1" ? $" BY {step}" : "";
        return $"{indent}FOR {varName} := {start} TO {end}{stepStr} DO{comment}";
    }

    private static string FormatForEach(Instruction instr, string indent, Stack<string> stack)
    {
        var itemName = GetVariableName(instr, "itemVariable");
        var collectionName = GetVariableName(instr, "collection");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}FOR_EACH {itemName} IN {collectionName} DO{comment}";
    }

    private static string FormatSwitch(Instruction instr, string indent, Stack<string> stack, Stack<string> valueStack)
    {
        var expr = GetExpression(instr, "expression");
        valueStack.Push(expr);
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}SWITCH {expr} DO{comment}";
    }

    private static string FormatCase(Instruction instr, string indent)
    {
        var value = GetExpression(instr, "value");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}CASE {value}:{comment}";
    }

    private static string FormatLabel(Instruction instr, string indent)
    {
        var labelName = GetFieldValue(instr, "labelName");
        return $"{indent}{labelName}:";
    }

    private static string FormatJumpLbl(Instruction instr, string indent)
    {
        var labelName = GetFieldValue(instr, "labelName");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}GOTO {labelName};{comment}";
    }

    private static string FormatJumpIf(Instruction instr, string indent)
    {
        var condition = GetExpression(instr, "condition");
        var labelName = GetFieldValue(instr, "labelName");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}IF {condition} THEN GOTO {labelName};{comment}";
    }

    private static string FormatCall(Instruction instr, string indent)
    {
        var progName = GetFieldValue(instr, "programName");
        var args = GetFieldValue(instr, "arguments");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        
        if (!string.IsNullOrEmpty(args))
            return $"{indent}CALL {progName}({args});{comment}";
        else
            return $"{indent}CALL {progName};{comment}";
    }

    private static string FormatReturn(Instruction instr, string indent)
    {
        var value = GetExpression(instr, "value");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        
        if (!string.IsNullOrEmpty(value))
            return $"{indent}RETURN {value};{comment}";
        else
            return $"{indent}RETURN;{comment}";
    }

    private static string FormatWait(Instruction instr, string indent)
    {
        var time = GetExpression(instr, "timeMs");
        var condition = GetExpression(instr, "condition");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        
        if (!string.IsNullOrEmpty(condition))
            return $"{indent}WAIT FOR {condition};{comment}";
        else if (!string.IsNullOrEmpty(time))
            return $"{indent}WAIT {time}ms;{comment}";
        else
            return $"{indent}WAIT;{comment}";
    }

    private static string FormatThrow(Instruction instr, string indent)
    {
        var alarmCode = GetFieldValue(instr, "alarmCode");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}THROW {alarmCode};{comment}";
    }

    private static string FormatCatch(Instruction instr, string indent)
    {
        var varName = GetVariableName(instr, "exceptionVariable");
        var alarmCode = GetFieldValue(instr, "alarmCode");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        
        if (!string.IsNullOrEmpty(varName))
            return $"{indent}CATCH {varName} FROM {alarmCode} DO{comment}";
        else if (!string.IsNullOrEmpty(alarmCode))
            return $"{indent}CATCH {alarmCode} DO{comment}";
        else
            return $"{indent}CATCH DO{comment}";
    }

    private static string FormatAlarmRaise(Instruction instr, string indent)
    {
        var alarmCode = GetFieldValue(instr, "alarmCode");
        var args = GetFieldValue(instr, "arguments");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        
        if (!string.IsNullOrEmpty(args))
            return $"{indent}RAISE_ALARM {alarmCode}, {args};{comment}";
        else
            return $"{indent}RAISE_ALARM {alarmCode};{comment}";
    }

    private static string FormatAlarmClear(Instruction instr, string indent)
    {
        var alarmCode = GetFieldValue(instr, "alarmCode");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}CLEAR_ALARM {alarmCode};{comment}";
    }

    private static string FormatAlarmAck(Instruction instr, string indent)
    {
        var alarmCode = GetFieldValue(instr, "alarmCode");
        var comment = !string.IsNullOrEmpty(instr.Comment) ? $"  ! {instr.Comment}" : "";
        return $"{indent}ACK_ALARM {alarmCode};{comment}";
    }

    #endregion

    #region Утилиты

    private static string GetVariableName(Instruction instr, string fieldName)
    {
        if (!instr.Fields.TryGetValue(fieldName, out var field))
            return "unknown";

        return field switch
        {
            VariableFieldValue v => v.VariableName,
            ExpressionFieldValue e => e.Expression,
            _ => field.ToString() ?? "unknown"
        };
    }

    private static string GetExpression(Instruction instr, string fieldName)
    {
        if (!instr.Fields.TryGetValue(fieldName, out var field))
            return "unknown";

        return field switch
        {
            ConstantFieldValue c => FormatValue(c.Value),
            VariableFieldValue v => v.VariableName,
            ExpressionFieldValue e => e.Expression,
            FunctionCallFieldValue f => $"{f.FunctionName}({string.Join(", ", f.Arguments.Select(FormatValue))})",
            _ => field.ToString() ?? "unknown"
        };
    }

    private static string GetFieldValue(Instruction instr, string fieldName)
    {
        if (!instr.Fields.TryGetValue(fieldName, out var field))
            return "";

        return field switch
        {
            ConstantFieldValue c => FormatValue(c.Value),
            VariableFieldValue v => v.VariableName,
            ExpressionFieldValue e => e.Expression,
            FunctionCallFieldValue f => $"{f.FunctionName}({string.Join(", ", f.Arguments.Select(FormatValue))})",
            LabelFieldValue l => l.LabelName,
            EnumFieldValue e => e.SelectedValue,
            _ => field.ToString() ?? ""
        };
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "NULL";
        if (value is string s) return $"'{s}'";
        if (value is bool b) return b ? "TRUE" : "FALSE";
        if (value is System.Collections.IEnumerable list && value is not string)
        {
            var items = list.Cast<object>().Select(FormatValue);
            return $"[{string.Join(", ", items)}]";
        }
        if (value is Dictionary<string, object> dict)
        {
            var parts = dict.Select(kvp => $"{kvp.Key}: {FormatValue(kvp.Value)}");
            return $"{{{string.Join(", ", parts)}}}";
        }
        return value.ToString() ?? "NULL";
    }

    private static bool IsClosingInstruction(string formId, out string? openFormId)
    {
        openFormId = formId switch
        {
            "core.endif" => "core.if",
            "core.endwhile" => "core.while",
            "core.endfor" => "core.for",
            "core.endforeach" => "core.foreach",
            "core.endswitch" => "core.switch",
            "core.endtry" => "core.try",
            _ => null
        };
        return openFormId != null;
    }

    private static string EscapeComment(string? comment)
    {
        if (string.IsNullOrEmpty(comment)) return "";
        return comment.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }

    #endregion
}
