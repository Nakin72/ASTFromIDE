// AstroEditor � ������ ������������ ����� �����������
// ����: Data > Binding > Execution
// ��� 30+ ����������, ������, ����������, �������, ������������, ���������������, ������������

using AstroEditor.Core.Common;
using AstroEditor.Core.Types;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Serialization;
using AstroEditor.Core.Data;
using AstroEditor.Core.Binding;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Common;

Console.WriteLine("�======================================================�");
Console.WriteLine("�     ASTRO EDITOR � ������ ������������             �");
Console.WriteLine("L======================================================-\n");

// ===================================================================
// 1. ������������� �������
// ===================================================================
Console.WriteLine("=== 1. ������������� ������� ===");
var project = new ProjectManager();
var baseFolder = Path.Combine(Environment.CurrentDirectory, "AstroData");
project.InitializeNew(baseFolder);

Console.WriteLine($"  ������ �����: {project.TypeRegistry.AllTypes.Count} �����");
Console.WriteLine($"  ������ ����:  {project.FormRegistry.AllForms.Count} ����");
Console.WriteLine();

// ===================================================================
// 2. ���� ������ (���������, enum, struct, alias)
// ===================================================================
Console.WriteLine("=== 2. ���� ������ ===");

// 2a. ��������� � ��������
Console.WriteLine("  --- ��������� ---");
foreach (var t in project.TypeRegistry.AllTypes.OfType<PrimitiveDataType>())
{
    var c = t.BuiltinConstraints;
    Console.WriteLine($"    {t.Name,-8} ({t.Id,-6}) > [{c?.Min?.ToString() ?? "�"}, {c?.Max?.ToString() ?? "�"}]");
}

// 2b. ���������������� Enum
Console.WriteLine("  --- ���������������� Enum ---");
var colorType = new EnumDataType
{
    Id = "color",
    Name = "COLOR",
    Category = DataTypeCategory.User,
    Values = new Dictionary<string, long> { ["RED"] = 0, ["GREEN"] = 1, ["BLUE"] = 2, ["YELLOW"] = 3 }
};
project.TypeRegistry.RegisterType(colorType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    ���: {colorType.Name} ({colorType.Values.Count} ��������)");

// 2c. ���������������� Struct
Console.WriteLine("  --- ���������������� Struct ---");
var pointType = new StructDataType
{
    Id = "point",
    Name = "POINT",
    Category = DataTypeCategory.User,
    Fields = new List<StructField>
    {
        new() { Name = "X", TypeId = "double" },
        new() { Name = "Y", TypeId = "double" },
        new() { Name = "Z", TypeId = "double" }
    }
};
project.TypeRegistry.RegisterType(pointType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    ���: {pointType.Name} (�����: {pointType.Fields.Count})");

// 2d. Alias
var speedType = new AliasDataType
{
    Id = "speed", Name = "SPEED", Category = DataTypeCategory.User, BaseTypeId = "int"
};
project.TypeRegistry.RegisterType(speedType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    Alias: {speedType.Name} > base={speedType.BaseTypeId}\n");

// ===================================================================
// 3. ���������� � ������������
// ===================================================================
Console.WriteLine("=== 3. ���������� � ������������ ===");

var intType = project.TypeRegistry.GetTypeById("int")!;
var realType = project.TypeRegistry.GetTypeById("real")!;
var boolType = project.TypeRegistry.GetTypeById("bool")!;
var stringType = project.TypeRegistry.GetTypeById("string")!;
var doubleType = project.TypeRegistry.GetTypeById("double")!;

// ���������� ���������� ������ �����
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("GlobalCounter", intType, 0));
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("Sensor1", intType, 0));
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("Sensor2", intType, 0));
project.GlobalTables.GetOrCreateTable(realType).AddVariable(new Variable("Pi", realType, 3.14159));
project.GlobalTables.GetOrCreateTable(stringType).AddVariable(new Variable("Status", stringType, "Idle"));
project.GlobalTables.GetOrCreateTable(colorType).AddVariable(new Variable("SelectedColor", colorType, 0L));

// �������� <=> (������������ �������������)
var binding = BindingManager.Bind("MyAlias", "GlobalCounter", BindingDirection.Bidirectional);
Console.WriteLine("  ��������: MyAlias <=> GlobalCounter (Bidirectional)");
// ���������: alias ������ �� target
Console.WriteLine($"    GlobalCounter = {GetGlobalVar(project, "GlobalCounter")}, MyAlias ������� = {binding.IsActive}");

// �������� OneWayToTarget: Sensor1 ����� � GlobalCounter
var binding2 = BindingManager.Bind("Sensor1", "GlobalCounter", BindingDirection.OneWayToTarget);
Console.WriteLine($"  ��������: Sensor1 => GlobalCounter (OneWayToTarget)");
Console.WriteLine();

// ��������������� �������
static object? GetGlobalVar(ProjectManager pm, string name)
{
    foreach (var t in pm.GlobalTables.Tables.Values)
        foreach (var v in t.Variables)
            if (v.Name == name) return v.Value;
    return null;
}

// ===================================================================
// 4. ��������� � ������ ����� ����������
// ===================================================================
Console.WriteLine("=== 4. ��������� � ������ ����� ���������� ===");

var program = new AstroProgram
{
    Name = "MainProgram", Author = "Demo", Description = "������ ������������",
    Version = "4.0", ReturnTypeId = "int", IsBackground = false, MaxCycles = 1000
};

// ��������� (������� enum)
program.Arguments.Add(new Argument { Name = "StartValue", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
program.Arguments.Add(new Argument { Name = "ColorArg", TypeId = "color", Direction = ArgumentDirection.In, DefaultValue = 0L });

// ��������� ����������
program.AddLocalVariable(new Variable("Counter", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Sum", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Temp", realType, 0.0), project.TypeRegistry);
program.AddLocalVariable(new Variable("IsEven", boolType, false), project.TypeRegistry);
program.AddLocalVariable(new Variable("X", doubleType, 0.0), project.TypeRegistry);

int line = 1;
void I(string fId, Dictionary<string, FieldValue>? f = null, string? c = null) =>
    program.Lines.Add(new Instruction(line++, fId) { Fields = f ?? new(), Comment = c ?? "" });

// --- Assign ---
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["expression"] = new ExpressionFieldValue("StartValue") }, "������� = ��������");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ConstantFieldValue(0) }, "Sum = 0");

// --- LBL + JumpIf + JumpLbl (������ ����) ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("START") });
I("core.jumpif", new() { ["condition"] = new ExpressionFieldValue("Counter >= 6"), ["labelName"] = new ConstantFieldValue("END") }, "����� ��� >= 6");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("Sum + Counter") });

// --- IF/ELSE/ENDIF ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"), ["expression"] = new ConstantFieldValue(true) }, "׸�");
I("core.else");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"), ["expression"] = new ConstantFieldValue(false) }, "�����");
I("core.endif");

// --- SWITCH/CASE/DEFAULT/ENDSWITCH ---
I("core.switch", new() { ["expression"] = new ExpressionFieldValue("Counter") });
I("core.case", new() { ["value"] = new ConstantFieldValue(2) });
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"), ["expression"] = new ConstantFieldValue(100.0) }, "Case 2");
I("core.default");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"), ["expression"] = new ConstantFieldValue(0.0) }, "Default");
I("core.endswitch");

// --- FOR/ENDFOR (��������� ����) ---
I("core.for", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["start"] = new ExpressionFieldValue("Counter + 1"), ["end"] = new ExpressionFieldValue("Counter + 3"), ["step"] = new ExpressionFieldValue("1") }, "FOR �����.");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("Sum + Counter") });

// --- BREAK (��� ���������� Sum > 50) ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("Sum > 50") });
I("core.break", new() {}, "Break ��� Sum>50");
I("core.endif");

I("core.endfor");

// --- ��������� Counter ---
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["expression"] = new ExpressionFieldValue("Counter + 1") });

// --- CONTINUE (���������� ������ ��������) ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });
I("core.continue", new() {}, "������� ������");
I("core.endif");

I("core.jumplbl", new() { ["labelName"] = new ConstantFieldValue("START") });

// --- END + Return ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("END") });
I("core.return", new() { ["value"] = new ExpressionFieldValue("Sum") });

program.Labels["START"] = 3; program.Labels["END"] = 28;
project.AddProgram(program);
Console.WriteLine($"  '{program.Name}': {program.Lines.Count} ����������\n");

// ===================================================================
// 5. ������������ (CALL)
// ===================================================================
Console.WriteLine("=== 5. ������������ (core.call) ===");

var subProg = new AstroProgram
{
    Name = "Multiply", ReturnTypeId = "int", IsBackground = false, MaxCycles = 10
};
subProg.Arguments.Add(new Argument { Name = "A", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
subProg.Arguments.Add(new Argument { Name = "B", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
subProg.AddLocalVariable(new Variable("Result", intType, 0), project.TypeRegistry);
subProg.Lines.Add(new Instruction(1, "core.assign")
{
    Fields = new() { ["variable"] = new VariableFieldValue("LocalVariables", "Result", "int"), ["expression"] = new ExpressionFieldValue("A * B") }
});
subProg.Lines.Add(new Instruction(2, "core.return")
{
    Fields = new() { ["value"] = new ExpressionFieldValue("Result") }
});
project.AddProgram(subProg);
Console.WriteLine($"  '{subProg.Name}': {subProg.Lines.Count} ����������\n");

// ===================================================================
// 6. ������ (������ ������������)
// ===================================================================
Console.WriteLine("=== 6. ������ (������ ������������) ===");

var alarms = project.Alarms;
alarms.CreateUserAlarm("SAFETY_DOOR", "Safety door is open on line {0}", AlarmSeverity.Fatal);
alarms.CreateUserAlarm("TOOL_WEAR", "Tool wear {0}% exceeded limit", AlarmSeverity.Warning);
alarms.CreateUserAlarm("PART_OK", "Part quality check passed", AlarmSeverity.Info);

Console.WriteLine($"  �����������: {alarms.Definitions.Count}");

// Raise � �����������
alarms.RaiseFromProgram(1001, "MainProgram", 15, "Main");
var def1001 = alarms.GetDefinition(1001);
Console.WriteLine($"  Active: {alarms.ActiveAlarms.Count}, Msg: \"{def1001?.FormatMessage(new object[] { "Main" })}\"");

// Raise � ���������� (Tool Wear 95%)
alarms.Raise(1002, 95.0);
var def1002 = alarms.GetDefinition(1002);
Console.WriteLine($"  #{1002}: \"{def1002?.FormatMessage(new object[] { 95.0 })}\"");

// Ack + Clear
alarms.Acknowledge(1002);
Console.WriteLine($"  ToolWear ����� Ack: {alarms.ActiveAlarms[1002].State}");
alarms.Clear(1002);
Console.WriteLine($"  ����� Clear: active={alarms.ActiveAlarms.Count}");

// ��������� > ���������� �����������
Console.Write("  ��������� SAFETY_DOOR: ");
try { alarms.Raise(1001, "Main"); }
catch (AlarmFatalException ex) { Console.WriteLine($"����������� > {ex.Message}"); }

// ClearAll
alarms.Raise(1002, 50.0);
alarms.ClearAll();
Console.WriteLine($"  ����� ClearAll: active={alarms.ActiveAlarms.Count}");
Console.WriteLine();

// ===================================================================
// 7. ���������� (OnChange, Background, OnAlarm)
// ===================================================================
Console.WriteLine("=== 7. ���������� (������ ������������) ===");

var interrupts = project.Interrupts;

// OnAlarm > Deferred
var intOnAlarm = new InterruptDefinition
{
    Id = "int-alarm", Name = "OnSafetyAlarm", TriggerType = InterruptTrigger.OnAlarm,
    AlarmCode = 1001, ExecutionMode = InterruptExecutionMode.Deferred, IsEnabled = true
};
interrupts.Register(intOnAlarm);

// OnValue > Background
var intBackground = new InterruptDefinition
{
    Id = "int-bg", Name = "BgSensorCheck", TriggerType = InterruptTrigger.OnValue,
    Expression = "Sensor1 > 5", ExecutionMode = InterruptExecutionMode.Background,
    IsEnabled = true, HandlerProgramName = "Multiply"
};
interrupts.Register(intBackground);

// OnRisingEdge > Inline
var intRising = new InterruptDefinition
{
    Id = "int-rise", Name = "OnRisingSensor2", TriggerType = InterruptTrigger.OnRisingEdge,
    VariableName = "Sensor2", ExecutionMode = InterruptExecutionMode.Inline, IsEnabled = true
};
interrupts.Register(intRising);

Console.WriteLine($"  ����������: {interrupts.Definitions.Count}");
foreach (var d in interrupts.Definitions.Values)
    Console.WriteLine($"    {d.Name,-20} | {d.TriggerType,-12} {d.ExecutionMode,-12} Enabled={d.IsEnabled}");

// Fire + Dequeue Deferred
interrupts.Fire(intOnAlarm);
Console.WriteLine($"  HasDeferred: {interrupts.HasDeferred}");
var dq = interrupts.DequeueDeferred();
Console.WriteLine($"  Dequeued: {dq?.Name}");
Console.WriteLine();

// ===================================================================
// 8. ������� + ���������� OnTimer
// ===================================================================
Console.WriteLine("=== 8. ������� + OnTimer ���������� ===");

var timers = project.Timers;
int timerCnt = 0;
timers.OnTimerElapsed += (t) =>
{
    timerCnt++;
    Console.WriteLine($"    ? {t.Name} # {t.ElapsedCount}");
};

// ������������� ������ (250ms)
timers.Register(new TimerDefinition
{
    Name = "Periodic250", IntervalMs = 250, Mode = TimerMode.Periodic
});

// Oneshot ������ (500ms)
timers.Register(new TimerDefinition
{
    Name = "Oneshot500", IntervalMs = 500, Mode = TimerMode.Oneshot
});

// ������ � �����������
var timerWithInt = new TimerDefinition
{
    Name = "TimerWithInt", IntervalMs = 300, Mode = TimerMode.Periodic
};
timers.Register(timerWithInt);

// ������ ���������� OnTimer ��� ����� �������
var intOnTimer = new InterruptDefinition
{
    Id = "int-timer", Name = "OnTimerElapsed", TriggerType = InterruptTrigger.OnTimer,
    TimerIntervalMs = 300, ExecutionMode = InterruptExecutionMode.Deferred, IsEnabled = true
};
interrupts.Register(intOnTimer);

Console.WriteLine("  �������� 1.2�...");
Thread.Sleep(1200);
timers.Disable("Periodic250");
timers.Disable("TimerWithInt");

Console.WriteLine($"  ������������ Periodic250: ~4 (����: {timerCnt})");
Console.WriteLine($"  Oneshot500 ��������: {timers.Timers.Values.FirstOrDefault(t => t.Name == "Oneshot500")?.ElapsedCount > 0}");
Console.WriteLine();

// ===================================================================
// 9. ������ � ������ STRUCT
// ===================================================================
Console.WriteLine("=== 9. ���� ��������� POINT ===");

// ������ ����������-���������
var table = project.GlobalTables.GetOrCreateTable(pointType);
var pointVar = new Variable("CurrentPos", pointType, new Dictionary<string, object>
{
    ["X"] = 100.5, ["Y"] = 200.3, ["Z"] = 50.0
});
table.AddVariable(pointVar);

if (pointVar.Value is Dictionary<string, object> pos)
{
    Console.WriteLine($"  CurrentPos = ({pos["X"]}, {pos["Y"]}, {pos["Z"]})");
    pos["X"] = 150.0;
    Console.WriteLine($"  ����� X = 150: ({pos["X"]}, {pos["Y"]}, {pos["Z"]})");
}
Console.WriteLine();

// ===================================================================
// 10. ���������� ���������
// ===================================================================
Console.WriteLine("=== 10. ���������� ��������� ===");

project.SaveAll();
var loadProject = new ProjectManager();
loadProject.Open(baseFolder);

Console.WriteLine($"  ����� ��������: �����={loadProject.TypeRegistry.AllTypes.Count}, ����={loadProject.FormRegistry.AllForms.Count}");

var interpreter = loadProject.CreateInterpreter();
var prog = loadProject.Programs["MainProgram"];

interpreter.Context.OnBeforeInstruction += (_, instr) =>
    Console.WriteLine($"    [{instr.LineNumber,2}] {instr.FormId,-15} | {instr.Comment}");

Console.WriteLine("  --- ������ MainProgram ---");
// ����� �������� �� LoadProgram (����� DefaultValue)
prog.Arguments.First(a => a.Name == "StartValue").DefaultValue = 1;
interpreter.LoadProgram(prog);
interpreter.Run();
Console.WriteLine($"  ���������: {interpreter.State.ReturnValue}\n");

// ===================================================================
// 11. CALL (����� ������������ �������)
// ===================================================================
Console.WriteLine("=== 11. ����� ������������ Multiply ===");

var subInterpreter = loadProject.CreateInterpreter();
var multiply = loadProject.Programs["Multiply"];

subInterpreter.Context.OnBeforeInstruction += (_, instr) =>
    Console.WriteLine($"    [{instr.LineNumber}] {instr.FormId}");

// ����� ��������� �� LoadProgram
multiply.Arguments.First(a => a.Name == "A").DefaultValue = 7;
multiply.Arguments.First(a => a.Name == "B").DefaultValue = 6;
subInterpreter.LoadProgram(multiply);
subInterpreter.Run();
Console.WriteLine($"  7 * 6 = {subInterpreter.State.ReturnValue}\n");

// ===================================================================
// 12. ����������� (Foreground + Background)
// ===================================================================
Console.WriteLine("=== 12. ��������������� ===");

var sched = loadProject.CreateScheduler();
sched.OnTaskStarted += (s) => Console.WriteLine($"  [{s.TaskId}] {s.Name} � ��������");
sched.OnTaskStopped += (s) => Console.WriteLine($"  [{s.TaskId}] {s.Name} � �����������");

// Foreground � MainProgram
sched.StartTask(new TaskConfig
{
    TaskId = 1, Name = "MainTask", Program = prog,
    Type = TaskType.Foreground, Priority = TaskPriority.Normal
});

// Background � WatchDog
var bgProg = new AstroProgram { Name = "BG", IsBackground = true, MaxCycles = 6 };
bgProg.AddLocalVariable(new Variable("Tick", intType, 0), loadProject.TypeRegistry);
bgProg.Lines.Add(new Instruction(1, "core.assign")
{
    Fields = new() { ["variable"] = new VariableFieldValue("LocalVariables", "Tick", "int"), ["expression"] = new ExpressionFieldValue("Tick + 1") },
    Comment = "������� ���"
});
loadProject.AddProgram(bgProg);

sched.StartTask(new TaskConfig
{
    TaskId = 2, Name = "BG", Program = bgProg,
    Type = TaskType.Background, Priority = TaskPriority.Low,
    CycleIntervalMs = 80, MaxCycles = 6
});

sched.StartScheduler();
Thread.Sleep(400);
sched.StopScheduler();
Console.WriteLine();

// ===================================================================
// 13. ������������ � �������� ������
// ===================================================================
Console.WriteLine("=== 13. ������������ � �������� ������ ===");

if (Directory.Exists(baseFolder))
{
    var allFiles = Directory.GetFiles(baseFolder, "*.*", SearchOption.AllDirectories);
    Console.WriteLine($"  ������ � {baseFolder}: {allFiles.Length}");
    foreach (var f in allFiles)
        Console.WriteLine($"    {Path.GetRelativePath(baseFolder, f)}");
}
Console.WriteLine();

// ===================================================================
// ����
// ===================================================================
Console.WriteLine("�======================================================�");
Console.WriteLine("�              �������� ������                       �");
Console.WriteLine("L======================================================-");
Console.WriteLine($"  ���� ������:     {loadProject.TypeRegistry.AllTypes.Count}");
Console.WriteLine($"  ���������� ����: {loadProject.FormRegistry.AllForms.Count}");
Console.WriteLine($"  ��������:        {loadProject.Programs.Count}");
Console.WriteLine($"  ������ (���.):   {loadProject.Alarms.Definitions.Count}");
Console.WriteLine($"  ����������:      {loadProject.Interrupts.Definitions.Count}");
Console.WriteLine($"  ��������:        {loadProject.Timers.Timers.Count}");
Console.WriteLine($"  ����. ������:    {loadProject.GlobalTables.Tables.Count}");
Console.WriteLine($"  ���������:       {interpreter.State.ReturnValue}");

Console.WriteLine("\n��������� ��������� ����������:");
foreach (var t in prog.LocalTables.Tables.Values)
    foreach (var v in t.Variables)
        Console.WriteLine($"  {v.Name,-15} = {v.Value}");

Console.WriteLine("\n��������� ���������� ����������:");
foreach (var t in loadProject.GlobalTables.Tables.Values)
    foreach (var v in t.Variables)
        Console.WriteLine($"  {v.Name,-15} = {v.Value}");

Console.WriteLine("\n? ������ ���� Data > Binding > Execution ��������!");
Console.WriteLine("   ��� 30+ ����������, ������, ����������, �������, ������������, CALL, ������������.");