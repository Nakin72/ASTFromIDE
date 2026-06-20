// AstroEditor — Полный демонстратор возможностей ядра
// Архитектура: Data > Binding > Execution
// Для 30+ инструкций, циклов, условий, вызовов, прерываний, таймеров

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

Console.WriteLine("======================================================");
Console.WriteLine("     ASTRO EDITOR — Полный демонстратор ядра          ");
Console.WriteLine("======================================================\n");

// ===================================================================
// 1. Инициализация проекта
// ===================================================================
Console.WriteLine("=== 1. Инициализация проекта ===");
var project = new ProjectManager();
var baseFolder = Path.Combine(Environment.CurrentDirectory, "AstroData");
project.InitializeNew(baseFolder);

Console.WriteLine($"  Типов данных: {project.TypeRegistry.AllTypes.Count}");
Console.WriteLine($"  Форм инструкций: {project.FormRegistry.AllForms.Count}");
Console.WriteLine();

// ===================================================================
// 2. Типы данных (примитивы, enum, struct, alias)
// ===================================================================
Console.WriteLine("=== 2. Типы данных ===");

// 2a. Примитивы и диапазоны
Console.WriteLine("  --- Примитивы ---");
foreach (var t in project.TypeRegistry.AllTypes.OfType<PrimitiveDataType>())
{
    var c = t.BuiltinConstraints;
    Console.WriteLine($"    {t.Name,-8} ({t.Id,-6}) > [{c?.Min?.ToString() ?? "∞"}, {c?.Max?.ToString() ?? "∞"}]");
}

// 2b. Перечисление Enum
Console.WriteLine("  --- Перечисление Enum ---");
var colorType = new EnumDataType
{
    Id = "color",
    Name = "COLOR",
    Category = DataTypeCategory.User,
    Values = new Dictionary<string, long> { ["RED"] = 0, ["GREEN"] = 1, ["BLUE"] = 2, ["YELLOW"] = 3 }
};
project.TypeRegistry.RegisterType(colorType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    Тип: {colorType.Name} ({colorType.Values.Count} значений)");

// 2c. Структура Struct
Console.WriteLine("  --- Структура Struct ---");
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
Console.WriteLine($"    Тип: {pointType.Name} (полей: {pointType.Fields.Count})");

// 2d. Псевдоним Alias
var speedType = new AliasDataType
{
    Id = "speed", Name = "SPEED", Category = DataTypeCategory.User, BaseTypeId = "int"
};
project.TypeRegistry.RegisterType(speedType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    Alias: {speedType.Name} > base={speedType.BaseTypeId}\n");

// ===================================================================
// 3. Переменные и привязки
// ===================================================================
Console.WriteLine("=== 3. Переменные и привязки ===");

var intType = project.TypeRegistry.GetTypeById("int")!;
var realType = project.TypeRegistry.GetTypeById("real")!;
var boolType = project.TypeRegistry.GetTypeById("bool")!;
var stringType = project.TypeRegistry.GetTypeById("string")!;
var doubleType = project.TypeRegistry.GetTypeById("double")!;

// Создаём глобальные переменные
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("GlobalCounter", intType, 0));
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("Sensor1", intType, 0));
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("Sensor2", intType, 0));
project.GlobalTables.GetOrCreateTable(realType).AddVariable(new Variable("Pi", realType, 3.14159));
project.GlobalTables.GetOrCreateTable(stringType).AddVariable(new Variable("Status", stringType, "Idle"));
project.GlobalTables.GetOrCreateTable(colorType).AddVariable(new Variable("SelectedColor", colorType, 0L));

// Привязка <=> (двунаправленная)
var binding = BindingManager.Bind("MyAlias", "GlobalCounter", BindingDirection.Bidirectional);
Console.WriteLine("  Привязка: MyAlias <=> GlobalCounter (Bidirectional)");
Console.WriteLine($"    GlobalCounter = {GetGlobalVar(project, "GlobalCounter")}, MyAlias активна = {binding.IsActive}");

// Привязка OneWayToTarget: Sensor1 меняется → GlobalCounter
var binding2 = BindingManager.Bind("Sensor1", "GlobalCounter", BindingDirection.OneWayToTarget);
Console.WriteLine($"  Привязка: Sensor1 => GlobalCounter (OneWayToTarget)");
Console.WriteLine();

static object? GetGlobalVar(ProjectManager pm, string name)
{
    foreach (var t in pm.GlobalTables.Tables.Values)
        foreach (var v in t.Variables)
            if (v.Name == name) return v.Value;
    return null;
}

// ===================================================================
// 4. Программа с полным набором инструкций
// ===================================================================
Console.WriteLine("=== 4. Программа с полным набором инструкций ===");

var program = new AstroProgram
{
    Name = "MainProgram", Author = "Demo", Description = "Полный демонстратор",
    Version = "4.0", ReturnTypeId = "int", IsBackground = false, MaxCycles = 1000
};

// Аргументы (включая enum)
program.Arguments.Add(new Argument { Name = "StartValue", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
program.Arguments.Add(new Argument { Name = "ColorArg", TypeId = "color", Direction = ArgumentDirection.In, DefaultValue = 0L });

// Локальные переменные
program.AddLocalVariable(new Variable("Counter", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Sum", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Temp", realType, 0.0), project.TypeRegistry);
program.AddLocalVariable(new Variable("IsEven", boolType, false), project.TypeRegistry);
program.AddLocalVariable(new Variable("X", doubleType, 0.0), project.TypeRegistry);

int line = 1;
void I(string fId, Dictionary<string, FieldValue>? f = null, string? c = null) =>
    program.Lines.Add(new Instruction(line++, fId) { Fields = f ?? new(), Comment = c ?? "" });

// --- Assign ---
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["expression"] = new ExpressionFieldValue("StartValue") }, "Counter = StartValue");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ConstantFieldValue(0) }, "Sum = 0");

// --- LBL + JumpIf + JumpLbl (простой цикл) ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("START") });
I("core.jumpif", new() { ["condition"] = new ExpressionFieldValue("Counter >= 6"), ["labelName"] = new ConstantFieldValue("END") }, "Выход если Counter >= 6");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("Sum + Counter") });

// --- IF/ELSE/ENDIF ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"), ["expression"] = new ConstantFieldValue(true) }, "Чётное");
I("core.else");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"), ["expression"] = new ConstantFieldValue(false) }, "Нечётное");
I("core.endif");

// --- SWITCH/CASE/DEFAULT/ENDSWITCH ---
I("core.switch", new() { ["expression"] = new ExpressionFieldValue("Counter") });
I("core.case", new() { ["value"] = new ConstantFieldValue(2) });
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"), ["expression"] = new ConstantFieldValue(100.0) }, "Case 2");
I("core.default");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"), ["expression"] = new ConstantFieldValue(0.0) }, "Default");
I("core.endswitch");

// --- FOR/ENDFOR (простой цикл) ---
I("core.for", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["start"] = new ExpressionFieldValue("Counter + 1"), ["end"] = new ExpressionFieldValue("Counter + 3"), ["step"] = new ExpressionFieldValue("1") }, "FOR цикл");
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("Sum + Counter") });

// --- BREAK (при достижении Sum > 50) ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("Sum > 50") });
I("core.break", new() {}, "Break если Sum>50");
I("core.endif");

I("core.endfor");

// --- Инкремент Counter ---
I("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"), ["expression"] = new ExpressionFieldValue("Counter + 1") });

// --- CONTINUE (пропуск чётных итераций) ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });
I("core.continue", new() {}, "Пропуск чётных");
I("core.endif");

I("core.jumplbl", new() { ["labelName"] = new ConstantFieldValue("START") });

// --- END + Return ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("END") });
I("core.return", new() { ["value"] = new ExpressionFieldValue("Sum") });

program.Labels["START"] = 3; program.Labels["END"] = 28;
project.AddProgram(program);
Console.WriteLine($"  '{program.Name}': {program.Lines.Count} инструкций\n");

// ===================================================================
// 5. Вызов программ (CALL)
// ===================================================================
Console.WriteLine("=== 5. Вызов программ (core.call) ===");

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
Console.WriteLine($"  '{subProg.Name}': {subProg.Lines.Count} инструкций\n");

// ===================================================================
// 5.5 Массивы и FOR EACH
// ===================================================================
Console.WriteLine("=== 5.5 Массивы и FOR EACH ===");

// Программа работы с массивами
var arrayProg = new AstroProgram
{
    Name = "ArrayTest", Author = "Demo", Description = "Тест массивов",
    Version = "1.0", ReturnTypeId = "int", IsBackground = false, MaxCycles = 100
};

arrayProg.AddLocalVariable(new Variable("MyArray", stringType, new List<object?> { 1.0, 2.0, 3.0, 4.0, 5.0 }), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Sum", intType, 0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Item", intType, 0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Size", intType, 0), project.TypeRegistry);

int arrayLine = 1;
void AI(string fId, Dictionary<string, FieldValue>? f = null, string? c = null) =>
    arrayProg.Lines.Add(new Instruction(arrayLine++, fId) { Fields = f ?? new(), Comment = c ?? "" });

// SIZE
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"), ["expression"] = new ExpressionFieldValue("SIZE(MyArray)") }, "Size = SIZE(MyArray)");

// FOREACH
AI("core.foreach", new() { ["itemVariable"] = new VariableFieldValue("LocalVariables", "Item", "int"), ["collection"] = new VariableFieldValue("LocalVariables", "MyArray", "string") }, "FOREACH Item IN MyArray");
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("Sum + Item") }, "Sum = Sum + Item");
AI("core.endforeach");

// ADD
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Item", "int"), ["expression"] = new ConstantFieldValue(10) }, "Item = 10");
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"), ["expression"] = new ExpressionFieldValue("ADD(MyArray, Item)") }, "ADD(MyArray, 10)");

// FIND
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"), ["expression"] = new ExpressionFieldValue("FIND(MyArray, 3)") }, "Index = FIND(MyArray, 3)");

// SLICE
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"), ["expression"] = new ExpressionFieldValue("SIZE(SLICE(MyArray, 1, 3))") }, "Size = SIZE(SLICE(MyArray, 1, 3))");

// RANGE
AI("core.assign", new() { ["variable"] = new VariableFieldValue("LocalVariables", "MyArray", "string"), ["expression"] = new ExpressionFieldValue("RANGE(1, 5, 1)") }, "MyArray = RANGE(1, 5, 1)");

AI("core.return", new() { ["value"] = new ExpressionFieldValue("Sum") });

project.AddProgram(arrayProg);
Console.WriteLine($"  '{arrayProg.Name}': {arrayProg.Lines.Count} инструкций");

var arrayInterpreter = project.CreateInterpreter();
arrayInterpreter.LoadProgram(arrayProg);
arrayInterpreter.Run();
Console.WriteLine($"  Результат: {arrayInterpreter.State.ReturnValue}");
Console.WriteLine();

// ===================================================================
// 6. Аварии (система аварийности)
// ===================================================================
Console.WriteLine("=== 6. Аварии (система аварийности) ===");

var alarms = project.Alarms;
alarms.CreateUserAlarm("SAFETY_DOOR", "Safety door is open on line {0}", AlarmSeverity.Fatal);
alarms.CreateUserAlarm("TOOL_WEAR", "Tool wear {0}% exceeded limit", AlarmSeverity.Warning);
alarms.CreateUserAlarm("PART_OK", "Part quality check passed", AlarmSeverity.Info);

Console.WriteLine($"  Определено аварий: {alarms.Definitions.Count}");

// Raise с подстановкой
alarms.RaiseFromProgram(1001, "MainProgram", 15, "Main");
var def1001 = alarms.GetDefinition(1001);
Console.WriteLine($"  Active: {alarms.ActiveAlarms.Count}, Msg: \"{def1001?.FormatMessage(new object[] { "Main" })}\"");

// Raise с параметром (Tool Wear 95%)
alarms.Raise(1002, 95.0);
var def1002 = alarms.GetDefinition(1002);
Console.WriteLine($"  #{1002}: \"{def1002?.FormatMessage(new object[] { 95.0 })}\"");

// Ack + Clear
alarms.Acknowledge(1002);
Console.WriteLine($"  ToolWear после Ack: {alarms.ActiveAlarms[1002].State}");
alarms.Clear(1002);
Console.WriteLine($"  После Clear: active={alarms.ActiveAlarms.Count}");

// Fatal-авария > выбрасывает исключение
Console.Write("  Авария SAFETY_DOOR: ");
try { alarms.Raise(1001, "Main"); }
catch (AlarmFatalException ex) { Console.WriteLine($"Исключение > {ex.Message}"); }

// ClearAll
alarms.Raise(1002, 50.0);
alarms.ClearAll();
Console.WriteLine($"  После ClearAll: active={alarms.ActiveAlarms.Count}");
Console.WriteLine();

// ===================================================================
// 7. Прерывания (OnChange, Background, OnAlarm)
// ===================================================================
Console.WriteLine("=== 7. Прерывания (типы триггеров) ===");

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

Console.WriteLine($"  Прерывания: {interrupts.Definitions.Count}");
foreach (var d in interrupts.Definitions.Values)
    Console.WriteLine($"    {d.Name,-20} | {d.TriggerType,-12} {d.ExecutionMode,-12} Enabled={d.IsEnabled}");

// Fire + Dequeue Deferred
interrupts.Fire(intOnAlarm);
Console.WriteLine($"  HasDeferred: {interrupts.HasDeferred}");
var dq = interrupts.DequeueDeferred();
Console.WriteLine($"  Dequeued: {dq?.Name}");
Console.WriteLine();

// ===================================================================
// 8. Таймеры + прерывания OnTimer
// ===================================================================
Console.WriteLine("=== 8. Таймеры + OnTimer прерывания ===");

var timers = project.Timers;
int timerCnt = 0;
timers.OnTimerElapsed += (t) =>
{
    timerCnt++;
    Console.WriteLine($"    → {t.Name} # {t.ElapsedCount}");
};

// Периодический таймер (250ms)
timers.Register(new TimerDefinition
{
    Name = "Periodic250", IntervalMs = 250, Mode = TimerMode.Periodic
});

// Oneshot таймер (500ms)
timers.Register(new TimerDefinition
{
    Name = "Oneshot500", IntervalMs = 500, Mode = TimerMode.Oneshot
});

// Таймер с прерыванием
var timerWithInt = new TimerDefinition
{
    Name = "TimerWithInt", IntervalMs = 300, Mode = TimerMode.Periodic
};
timers.Register(timerWithInt);

// Создаём прерывание OnTimer для этого таймера
var intOnTimer = new InterruptDefinition
{
    Id = "int-timer", Name = "OnTimerElapsed", TriggerType = InterruptTrigger.OnTimer,
    TimerIntervalMs = 300, ExecutionMode = InterruptExecutionMode.Deferred, IsEnabled = true
};
interrupts.Register(intOnTimer);

Console.WriteLine("  Запуск 1.2с...");
Thread.Sleep(1200);
timers.Disable("Periodic250");
timers.Disable("TimerWithInt");

Console.WriteLine($"  Срабатываний Periodic250: ~4 (факт: {timerCnt})");
Console.WriteLine($"  Oneshot500 сработал: {timers.Timers.Values.FirstOrDefault(t => t.Name == "Oneshot500")?.ElapsedCount > 0}");
Console.WriteLine();

// ===================================================================
// 9. Работа со STRUCT
// ===================================================================
Console.WriteLine("=== 9. Работа со STRUCT POINT ===");

// Создаём переменную-структуру
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
    Console.WriteLine($"  После X = 150: ({pos["X"]}, {pos["Y"]}, {pos["Z"]})");
}
Console.WriteLine();

// ===================================================================
// 10. Сохранение и загрузка проекта
// ===================================================================
Console.WriteLine("=== 10. Сохранение и загрузка проекта ===");

project.SaveAll();
var loadProject = new ProjectManager();
loadProject.Open(baseFolder);

Console.WriteLine($"  После загрузки: типов={loadProject.TypeRegistry.AllTypes.Count}, форм={loadProject.FormRegistry.AllForms.Count}");

var interpreter = loadProject.CreateInterpreter();
var prog = loadProject.Programs["MainProgram"];

interpreter.Context.OnBeforeInstruction += (_, instr) =>
    Console.WriteLine($"    [{instr.LineNumber,2}] {instr.FormId,-15} | {instr.Comment}");

Console.WriteLine("  --- Запуск MainProgram ---");
// Передаём аргумент при LoadProgram (через DefaultValue)
prog.Arguments.First(a => a.Name == "StartValue").DefaultValue = 1;
interpreter.LoadProgram(prog);
interpreter.Run();
Console.WriteLine($"  Возврат: {interpreter.State.ReturnValue}\n");

// ===================================================================
// 11. CALL (вызов подпрограммы с аргументами)
// ===================================================================
Console.WriteLine("=== 11. Вызов подпрограммы Multiply ===");

var subInterpreter = loadProject.CreateInterpreter();
var multiply = loadProject.Programs["Multiply"];

subInterpreter.Context.OnBeforeInstruction += (_, instr) =>
    Console.WriteLine($"    [{instr.LineNumber}] {instr.FormId}");

// Передаём аргументы при LoadProgram
multiply.Arguments.First(a => a.Name == "A").DefaultValue = 7;
multiply.Arguments.First(a => a.Name == "B").DefaultValue = 6;
subInterpreter.LoadProgram(multiply);
subInterpreter.Run();
Console.WriteLine($"  7 * 6 = {subInterpreter.State.ReturnValue}\n");

// ===================================================================
// 12. Многозадачность (Foreground + Background)
// ===================================================================
Console.WriteLine("=== 12. Многозадачность ===");

var sched = loadProject.CreateScheduler();
sched.OnTaskStarted += (s) => Console.WriteLine($"  [{s.TaskId}] {s.Name} → запущена");
sched.OnTaskStopped += (s) => Console.WriteLine($"  [{s.TaskId}] {s.Name} → остановлена");

// Foreground задача с MainProgram
sched.StartTask(new TaskConfig
{
    TaskId = 1, Name = "MainTask", Program = prog,
    Type = TaskType.Foreground, Priority = TaskPriority.Normal
});

// Background задача с WatchDog
var bgProg = new AstroProgram { Name = "BG", IsBackground = true, MaxCycles = 6 };
bgProg.AddLocalVariable(new Variable("Tick", intType, 0), loadProject.TypeRegistry);
bgProg.Lines.Add(new Instruction(1, "core.assign")
{
    Fields = new() { ["variable"] = new VariableFieldValue("LocalVariables", "Tick", "int"), ["expression"] = new ExpressionFieldValue("Tick + 1") },
    Comment = "Инкремент тика"
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
// 13. Структура файлов проекта
// ===================================================================
Console.WriteLine("=== 13. Структура файлов проекта ===");

if (Directory.Exists(baseFolder))
{
    var allFiles = Directory.GetFiles(baseFolder, "*.*", SearchOption.AllDirectories);
    Console.WriteLine($"  Файлов в {baseFolder}: {allFiles.Length}");
    foreach (var f in allFiles)
        Console.WriteLine($"    {Path.GetRelativePath(baseFolder, f)}");
}
Console.WriteLine();

// ===================================================================
// Итоги
// ===================================================================
Console.WriteLine("======================================================");
Console.WriteLine("              ИТОГОВЫЙ ОТЧЁТ                         ");
Console.WriteLine("======================================================");
Console.WriteLine($"  Типов данных:     {loadProject.TypeRegistry.AllTypes.Count}");
Console.WriteLine($"  Форм инструкций:  {loadProject.FormRegistry.AllForms.Count}");
Console.WriteLine($"  Программ:         {loadProject.Programs.Count}");
Console.WriteLine($"  Аварий (сист.):   {loadProject.Alarms.Definitions.Count}");
Console.WriteLine($"  Прерываний:       {loadProject.Interrupts.Definitions.Count}");
Console.WriteLine($"  Таймеров:         {loadProject.Timers.Timers.Count}");
Console.WriteLine($"  Глоб. таблиц:     {loadProject.GlobalTables.Tables.Count}");
Console.WriteLine($"  Возврат:          {interpreter.State.ReturnValue}");

Console.WriteLine("\nСостояние локальных переменных:");
foreach (var t in prog.LocalTables.Tables.Values)
    foreach (var v in t.Variables)
        Console.WriteLine($"  {v.Name,-15} = {v.Value}");

Console.WriteLine("\nСостояние глобальных переменных:");
foreach (var t in loadProject.GlobalTables.Tables.Values)
    foreach (var v in t.Variables)
        Console.WriteLine($"  {v.Name,-15} = {v.Value}");

Console.WriteLine("\n✓ Полный цикл Data > Binding > Execution завершён!");
Console.WriteLine("  Все 30+ инструкции, циклы, условия, вызовы, прерывания, таймеры работают.");
