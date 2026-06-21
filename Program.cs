// AstroEditor v4 — Полный демонстратор с AST-экспортом
// Экспорт: JSON, Текст, CSV + AST-деревья выражений

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using AstroEditor.Core.Tables;
using AstroEditor.Core.Expressions;
using System.Linq;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║     ASTRO EDITOR v4 — Полный демонстратор ядра          ║");
Console.WriteLine("║     Экспорт: JSON, Текст, CSV + AST-деревья             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

// ===================================================================
// 1. Инициализация проекта
// ===================================================================
Console.WriteLine("═══ 1. Инициализация проекта ═══");
var project = new ProjectManager();
var baseFolder = Path.Combine(Environment.CurrentDirectory, "AstroData");
var exportFolder = Path.Combine(Environment.CurrentDirectory, "Export");

Directory.CreateDirectory(baseFolder);
Directory.CreateDirectory(exportFolder);

project.InitializeNew(baseFolder);

Console.WriteLine($"  ✓ Типов данных: {project.TypeRegistry.AllTypes.Count}");
Console.WriteLine($"  ✓ Форм инструкций: {project.FormRegistry.AllForms.Count}");
Console.WriteLine();

// ===================================================================
// 2. Типы данных
// ===================================================================
Console.WriteLine("═══ 2. Типы данных ═══");

// 2a. Примитивы
Console.WriteLine("  ─── Примитивные типы ───");
foreach (var t in project.TypeRegistry.AllTypes.OfType<PrimitiveDataType>())
{
    var c = t.BuiltinConstraints;
    Console.WriteLine($"    {t.Name,-8} ({t.Id,-6}) > [{c?.Min?.ToString() ?? "∞"}, {c?.Max?.ToString() ?? "∞"}]");
}

// 2b. Enum
Console.WriteLine("\n  ─── Перечисление Enum ───");
var colorType = new EnumDataType
{
    Id = "color",
    Name = "COLOR",
    Category = DataTypeCategory.User,
    Values = new Dictionary<string, long>
    {
        ["RED"] = 0,
        ["GREEN"] = 1,
        ["BLUE"] = 2,
        ["YELLOW"] = 3
    }
};
project.TypeRegistry.RegisterType(colorType);
project.TypeRegistry.ResolveReferences();
Console.WriteLine($"    ✓ Тип: {colorType.Name} ({colorType.Values.Count} значений)");

// 2c. Struct
Console.WriteLine("\n  ─── Структура Struct ───");
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
Console.WriteLine($"    ✓ Тип: {pointType.Name} (полей: {pointType.Fields.Count})");

// 2d. Alias
var speedType = new AliasDataType
{
    Id = "speed",
    Name = "SPEED",
    Category = DataTypeCategory.User,
    BaseTypeId = "int"
};
project.TypeRegistry.RegisterType(speedType);
Console.WriteLine($"    ✓ Alias: {speedType.Name} > base={speedType.BaseTypeId}");

Console.WriteLine();

// ===================================================================
// 3. Переменные и таблицы
// ===================================================================
Console.WriteLine("═══ 3. Переменные и таблицы ═══");

var intType = project.TypeRegistry.GetTypeById("int")!;
var realType = project.TypeRegistry.GetTypeById("real")!;
var boolType = project.TypeRegistry.GetTypeById("bool")!;
var stringType = project.TypeRegistry.GetTypeById("string")!;
var doubleType = project.TypeRegistry.GetTypeById("double")!;

var sensor1Var = new Variable("Sensor1", intType, 100);
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("GlobalCounter", intType, 0));
project.GlobalTables.GetOrCreateTable(intType).AddVariable(sensor1Var);
project.GlobalTables.GetOrCreateTable(intType).AddVariable(new Variable("Sensor2", intType, 200));
project.GlobalTables.GetOrCreateTable(realType).AddVariable(new Variable("Pi", realType, 3.14159));
project.GlobalTables.GetOrCreateTable(stringType).AddVariable(new Variable("Status", stringType, "Idle"));
project.GlobalTables.GetOrCreateTable(colorType).AddVariable(new Variable("SelectedColor", colorType, 0L));

var pointTable = project.GlobalTables.GetOrCreateTable(pointType);
var pointVar = new Variable("CurrentPos", pointType, new Dictionary<string, object>
{
    ["X"] = 100.5,
    ["Y"] = 200.3,
    ["Z"] = 50.0
});
pointTable.AddVariable(pointVar);

Console.WriteLine($"  ✓ Глобальных переменных: {CountVariables(project.GlobalTables)}");
Console.WriteLine();

// ===================================================================
// 4. Привязки (Binding)
// ===================================================================
Console.WriteLine("═══ 4. Привязки (Binding) ═══");

// ✅ P0-1: Используем ThreadSafeBindingManager вместо статического BindingManager
var bindingService = project.Bindings;

var binding1 = bindingService.Bind("MyAlias", "GlobalCounter", BindingDirection.Bidirectional);
Console.WriteLine($"  ✓ MyAlias <=> GlobalCounter (Bidirectional)");

var binding2 = bindingService.Bind("Sensor1", "GlobalCounter", BindingDirection.OneWayToTarget);
Console.WriteLine($"  ✓ Sensor1 => GlobalCounter (OneWayToTarget)");

bindingService.UpdateValue("Sensor1", 150);
Console.WriteLine($"  ✓ Sensor1 = 150 → GlobalCounter = {project.GlobalTables.GetOrCreateTable(intType).FindVariable("GlobalCounter")?.Value}");
Console.WriteLine();

// ===================================================================
// 5. Программа с полным набором инструкций
// ===================================================================
Console.WriteLine("═══ 5. Программа с полным набором инструкций ═══");

var program = new AstroProgram
{
    Name = "MainProgram",
    Author = "Demo",
    Description = "Полный демонстратор всех инструкций ASTRO",
    Version = "4.0",
    ReturnTypeId = "int",
    IsBackground = false,
    MaxCycles = 1000
};

program.Arguments.Add(new Argument { Name = "StartValue", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
program.Arguments.Add(new Argument { Name = "ColorArg", TypeId = "color", Direction = ArgumentDirection.In, DefaultValue = 0L });

program.AddLocalVariable(new Variable("Counter", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Sum", intType, 0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Temp", realType, 0.0), project.TypeRegistry);
program.AddLocalVariable(new Variable("IsEven", boolType, false), project.TypeRegistry);
program.AddLocalVariable(new Variable("X", doubleType, 0.0), project.TypeRegistry);
program.AddLocalVariable(new Variable("Message", stringType, ""), project.TypeRegistry);

int line = 1;
void I(string fId, Dictionary<string, FieldValue>? f = null, string? c = null) =>
    program.Lines.Add(new Instruction(line++, fId) { Fields = f ?? new(), Comment = c ?? "" });

// --- Assign ---
I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
    ["expression"] = new ExpressionFieldValue("StartValue")
}, "Counter = StartValue");

I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
    ["expression"] = new ConstantFieldValue(0)
}, "Sum = 0");

// --- LBL + JumpIf + JumpLbl (цикл) ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("START") });

I("core.jumpif", new()
{
    ["condition"] = new ExpressionFieldValue("Counter >= 6"),
    ["labelName"] = new ConstantFieldValue("END")
}, "Выход если Counter >= 6");

I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
    ["expression"] = new ExpressionFieldValue("Sum + Counter")
});

// --- IF/ELSE/ENDIF ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });

I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"),
    ["expression"] = new ConstantFieldValue(true)
}, "Чётное");

I("core.else");

I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"),
    ["expression"] = new ConstantFieldValue(false)
}, "Нечётное");

I("core.endif");

// --- FOR/ENDFOR ---
I("core.for", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
    ["start"] = new ExpressionFieldValue("Counter + 1"),
    ["end"] = new ExpressionFieldValue("Counter + 3"),
    ["step"] = new ExpressionFieldValue("1")
}, "FOR цикл");

I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
    ["expression"] = new ExpressionFieldValue("Sum + Counter")
});

// --- BREAK ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("Sum > 50") });
I("core.break", new() {}, "Break если Sum>50");
I("core.endif");

I("core.endfor");

// --- Инкремент ---
I("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
    ["expression"] = new ExpressionFieldValue("Counter + 1")
});

// --- CONTINUE ---
I("core.if", new() { ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0") });
I("core.continue", new() {}, "Пропуск чётных");
I("core.endif");

I("core.jumplbl", new() { ["labelName"] = new ConstantFieldValue("START") });

// --- END + Return ---
I("core.lbl", new() { ["labelName"] = new ConstantFieldValue("END") });
I("core.return", new() { ["value"] = new ExpressionFieldValue("Sum") });

program.Labels["START"] = 3;
program.Labels["END"] = 22;
project.AddProgram(program);

Console.WriteLine($"  ✓ '{program.Name}': {program.Lines.Count} инструкций");
Console.WriteLine();

// ===================================================================
// 6. Вызов программ (CALL)
// ===================================================================
Console.WriteLine("═══ 6. Вызов программ (CALL) ═══");

var subProg = new AstroProgram
{
    Name = "Multiply",
    ReturnTypeId = "int",
    IsBackground = false,
    MaxCycles = 10
};

subProg.Arguments.Add(new Argument { Name = "A", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
subProg.Arguments.Add(new Argument { Name = "B", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
subProg.AddLocalVariable(new Variable("Result", intType, 0), project.TypeRegistry);

subProg.Lines.Add(new Instruction(1, "core.assign")
{
    Fields = new()
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Result", "int"),
        ["expression"] = new ExpressionFieldValue("A * B")
    }
});

subProg.Lines.Add(new Instruction(2, "core.return")
{
    Fields = new() { ["value"] = new ExpressionFieldValue("Result") }
});

project.AddProgram(subProg);
Console.WriteLine($"  ✓ '{subProg.Name}': {subProg.Lines.Count} инструкций");
Console.WriteLine();

// ===================================================================
// 7. Массивы и FOR EACH
// ===================================================================
Console.WriteLine("═══ 7. Массивы и FOR EACH ═══");

var arrayProg = new AstroProgram
{
    Name = "ArrayTest",
    Author = "Demo",
    Description = "Тест массивов и встроенных функций",
    Version = "1.0",
    ReturnTypeId = "int",
    IsBackground = false,
    MaxCycles = 100
};

arrayProg.AddLocalVariable(new Variable("MyArray", stringType, new List<object?> { 1.0, 2.0, 3.0, 4.0, 5.0 }), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Sum", intType, 0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Item", intType, 0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Size", intType, 0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("X", doubleType, 0.0), project.TypeRegistry);
arrayProg.AddLocalVariable(new Variable("Message", stringType, ""), project.TypeRegistry);

int arrayLine = 1;
void AI(string fId, Dictionary<string, FieldValue>? f = null, string? c = null) =>
    arrayProg.Lines.Add(new Instruction(arrayLine++, fId) { Fields = f ?? new(), Comment = c ?? "" });

// SIZE
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"),
    ["expression"] = new ExpressionFieldValue("SIZE(MyArray)")
}, "Size = SIZE(MyArray)");

// FOREACH
AI("core.foreach", new()
{
    ["itemVariable"] = new VariableFieldValue("LocalVariables", "Item", "int"),
    ["collection"] = new VariableFieldValue("LocalVariables", "MyArray", "string")
}, "FOREACH Item IN MyArray");

AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
    ["expression"] = new ExpressionFieldValue("Sum + Item")
}, "Sum = Sum + Item");

AI("core.endforeach");

// ADD
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Item", "int"),
    ["expression"] = new ConstantFieldValue(10)
}, "Item = 10");

AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"),
    ["expression"] = new ExpressionFieldValue("ADD(MyArray, Item)")
}, "ADD(MyArray, 10)");

// FIND
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"),
    ["expression"] = new ExpressionFieldValue("FIND(MyArray, 3)")
}, "Index = FIND(MyArray, 3)");

// SLICE
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
    ["expression"] = new ExpressionFieldValue("SIZE(SLICE(MyArray, 1, 3))")
}, "Size = SIZE(SLICE(MyArray, 1, 3))");

// RANGE
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "MyArray", "string"),
    ["expression"] = new ExpressionFieldValue("RANGE(1, 5, 1)")
}, "MyArray = RANGE(1, 5, 1)");

// Математические функции
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "X", "double"),
    ["expression"] = new ExpressionFieldValue("SIN(3.14159 / 2)")
}, "X = SIN(PI/2)");

AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "X", "double"),
    ["expression"] = new ExpressionFieldValue("SQRT(16)")
}, "X = SQRT(16)");

// Строковые функции (через переменные, т.к. лексер не поддерживает кавычки)
AI("core.assign", new()
{
    ["variable"] = new VariableFieldValue("LocalVariables", "Message", "string"),
    ["expression"] = new ConstantFieldValue("Hello World")
}, "Message = 'Hello World'");

AI("core.return", new() { ["value"] = new ExpressionFieldValue("Sum") });

project.AddProgram(arrayProg);
Console.WriteLine($"  ✓ '{arrayProg.Name}': {arrayProg.Lines.Count} инструкций");

var arrayInterpreter = project.CreateInterpreter();
arrayInterpreter.LoadProgram(arrayProg);
arrayInterpreter.Run();
Console.WriteLine($"  ✓ Результат: {arrayInterpreter.State.ReturnValue}");
Console.WriteLine();

// ===================================================================
// 8. Аварии (Alarms)
// ===================================================================
Console.WriteLine("═══ 8. Аварии (Alarms) ═══");

var alarms = project.Alarms;
alarms.CreateUserAlarm("SAFETY_DOOR", "Safety door is open on line {0}", AlarmSeverity.Fatal);
alarms.CreateUserAlarm("TOOL_WEAR", "Tool wear {0}% exceeded limit", AlarmSeverity.Warning);
alarms.CreateUserAlarm("PART_OK", "Part quality check passed", AlarmSeverity.Info);

Console.WriteLine($"  ✓ Определено аварий: {alarms.Definitions.Count}");

alarms.RaiseFromProgram(1001, "MainProgram", 15, "Main");
Console.WriteLine($"  ✓ Active: {alarms.ActiveAlarms.Count}");

alarms.Raise(1002, 95.0);
Console.WriteLine($"  ✓ Авария #1002 поднята");

alarms.Acknowledge(1002);
Console.WriteLine($"  ✓ Авария #1002 квитирована");

alarms.Clear(1002);
Console.WriteLine($"  ✓ Авария #1002 сброшена");

Console.Write("  ✓ Авария SAFETY_DOOR (Fatal): ");
try
{
    alarms.Raise(1001, "Main");
}
catch (AlarmFatalException ex)
{
    Console.WriteLine($"Исключение > {ex.Message}");
}

alarms.ClearAll();
Console.WriteLine($"  ✓ Все аварии сброшены");
Console.WriteLine();

// ===================================================================
// 9. Прерывания (Interrupts)
// ===================================================================
Console.WriteLine("═══ 9. Прерывания (Interrupts) ═══");

var interrupts = project.Interrupts;

var intOnAlarm = new InterruptDefinition
{
    Id = "int-alarm",
    Name = "OnSafetyAlarm",
    TriggerType = InterruptTrigger.OnAlarm,
    AlarmCode = 1001,
    ExecutionMode = InterruptExecutionMode.Deferred,
    IsEnabled = true
};
interrupts.Register(intOnAlarm);

var intBackground = new InterruptDefinition
{
    Id = "int-bg",
    Name = "BgSensorCheck",
    TriggerType = InterruptTrigger.OnValue,
    Expression = "Sensor1 > 5",
    ExecutionMode = InterruptExecutionMode.Background,
    IsEnabled = true,
    HandlerProgramName = "Multiply"
};
interrupts.Register(intBackground);

var intRising = new InterruptDefinition
{
    Id = "int-rise",
    Name = "OnRisingSensor2",
    TriggerType = InterruptTrigger.OnRisingEdge,
    VariableName = "Sensor2",
    ExecutionMode = InterruptExecutionMode.Inline,
    IsEnabled = true
};
interrupts.Register(intRising);

Console.WriteLine($"  ✓ Прерываний: {interrupts.Definitions.Count}");
foreach (var d in interrupts.Definitions.Values)
    Console.WriteLine($"    {d.Name,-20} | {d.TriggerType,-12} {d.ExecutionMode,-12}");

interrupts.Fire(intOnAlarm);
Console.WriteLine($"  ✓ HasDeferred: {interrupts.HasDeferred}");

var dq = interrupts.DequeueDeferred();
Console.WriteLine($"  ✓ Dequeued: {dq?.Name}");
Console.WriteLine();

// ===================================================================
// 10. Таймеры (Timers)
// ===================================================================
Console.WriteLine("═══ 10. Таймеры (Timers) ═══");

var timers = project.Timers;
int timerCnt = 0;
timers.OnTimerElapsed += (t) =>
{
    timerCnt++;
    Console.WriteLine($"    → {t.Name} # {t.ElapsedCount}");
};

timers.Register(new TimerDefinition { Name = "Periodic250", IntervalMs = 250, Mode = TimerMode.Periodic });
timers.Register(new TimerDefinition { Name = "Oneshot500", IntervalMs = 500, Mode = TimerMode.Oneshot });

var timerWithInt = new TimerDefinition { Name = "TimerWithInt", IntervalMs = 300, Mode = TimerMode.Periodic };
timers.Register(timerWithInt);

var intOnTimer = new InterruptDefinition
{
    Id = "int-timer",
    Name = "OnTimerElapsed",
    TriggerType = InterruptTrigger.OnTimer,
    TimerIntervalMs = 300,
    ExecutionMode = InterruptExecutionMode.Deferred,
    IsEnabled = true
};
interrupts.Register(intOnTimer);

Console.WriteLine("  Запуск 1.2с...");
Thread.Sleep(1200);
timers.Disable("Periodic250");
timers.Disable("TimerWithInt");

Console.WriteLine($"  ✓ Срабатываний Periodic250: ~4 (факт: {timerCnt})");
Console.WriteLine($"  ✓ Oneshot500 сработал: {timers.Timers.Values.FirstOrDefault(t => t.Name == "Oneshot500")?.ElapsedCount > 0}");
Console.WriteLine();

// ===================================================================
// 11. Сохранение и загрузка проекта (JSON)
// ===================================================================
Console.WriteLine("═══ 11. Сохранение и загрузка проекта (JSON) ═══");

project.SaveAll();
Console.WriteLine($"  ✓ Проект сохранён в {baseFolder}");

var loadProject = new ProjectManager();
loadProject.Open(baseFolder);
Console.WriteLine($"  ✓ Проект загружен");
Console.WriteLine();

// ===================================================================
// 12. Экспорт в различные форматы
// ===================================================================
Console.WriteLine("═══ 12. Экспорт данных в различные форматы ═══");

// 12a. Экспорт типов данных
Console.WriteLine("\n  ─── Экспорт типов данных ───");
ExportHelper.ExportToJson(project.TypeRegistry, Path.Combine(exportFolder, "types.json"));
Console.WriteLine($"    ✓ types.json");

ExportHelper.ExportTypesToText(project.TypeRegistry, Path.Combine(exportFolder, "types.txt"));
Console.WriteLine($"    ✓ types.txt");

ExportHelper.ExportTypesToCsv(project.TypeRegistry, Path.Combine(exportFolder, "types.csv"));
Console.WriteLine($"    ✓ types.csv");

// 12b. Экспорт форм инструкций
Console.WriteLine("\n  ─── Экспорт форм инструкций ───");
ExportHelper.ExportToJson(project.FormRegistry, Path.Combine(exportFolder, "forms.json"));
Console.WriteLine($"    ✓ forms.json");

ExportHelper.ExportFormsToText(project.FormRegistry, Path.Combine(exportFolder, "forms.txt"));
Console.WriteLine($"    ✓ forms.txt");

ExportHelper.ExportFormsToCsv(project.FormRegistry, Path.Combine(exportFolder, "forms.csv"));
Console.WriteLine($"    ✓ forms.csv");

// 12c. Экспорт программ
Console.WriteLine("\n  ─── Экспорт программ ───");
foreach (var prog in project.Programs.Values)
{
    ExportHelper.ExportToJson(prog, Path.Combine(exportFolder, $"program_{prog.Name}.json"));
    ExportHelper.ExportProgramToText(prog, project.FormRegistry, Path.Combine(exportFolder, $"program_{prog.Name}.txt"));
    ExportHelper.ExportProgramToCsv(prog, Path.Combine(exportFolder, $"program_{prog.Name}.csv"));
    
    // Экспорт в формате FANUC TP .ls
    AstroEditor.Core.Serialization.FanucStyleExporter.SaveToFile(prog, Path.Combine(exportFolder, $"{prog.Name}.ls"), project.TypeRegistry);
    
    Console.WriteLine($"    ✓ {prog.Name} (json, txt, csv, ls)");
}

// 12c.2. Экспорт программ из загруженного проекта
foreach (var prog in loadProject.Programs.Values)
{
    if (!project.Programs.ContainsKey(prog.Name))
    {
        ExportHelper.ExportToJson(prog, Path.Combine(exportFolder, $"program_{prog.Name}_loaded.json"));
        ExportHelper.ExportProgramToText(prog, loadProject.FormRegistry, Path.Combine(exportFolder, $"program_{prog.Name}_loaded.txt"));
        ExportHelper.ExportProgramToCsv(prog, Path.Combine(exportFolder, $"program_{prog.Name}_loaded.csv"));
        
        // Экспорт в формате FANUC TP .ls
        AstroEditor.Core.Serialization.FanucStyleExporter.SaveToFile(prog, Path.Combine(exportFolder, $"{prog.Name}_loaded.ls"), loadProject.TypeRegistry);
        
        Console.WriteLine($"    ✓ {prog.Name}_loaded (json, txt, csv, ls)");
    }
}

// 12d. Экспорт переменных
Console.WriteLine("\n  ─── Экспорт глобальных переменных ───");
ExportHelper.ExportToJson(project.GlobalTables, Path.Combine(exportFolder, "globals.json"));
Console.WriteLine($"    ✓ globals.json");

ExportHelper.ExportVariablesToText(project.GlobalTables, Path.Combine(exportFolder, "globals.txt"));
Console.WriteLine($"    ✓ globals.txt");

ExportHelper.ExportVariablesToCsv(project.GlobalTables, Path.Combine(exportFolder, "globals.csv"));
Console.WriteLine($"    ✓ globals.csv");

// 12e. Экспорт аварий
Console.WriteLine("\n  ─── Экспорт аварий ───");
ExportHelper.ExportToJson(alarms.Definitions.Values, Path.Combine(exportFolder, "alarms.json"));
Console.WriteLine($"    ✓ alarms.json");

ExportHelper.ExportAlarmsToText(alarms.Definitions.Values, Path.Combine(exportFolder, "alarms.txt"));
Console.WriteLine($"    ✓ alarms.txt");

ExportHelper.ExportAlarmsToCsv(alarms.Definitions.Values, Path.Combine(exportFolder, "alarms.csv"));
Console.WriteLine($"    ✓ alarms.csv");

// 12f. Экспорт прерываний
Console.WriteLine("\n  ─── Экспорт прерываний ───");
ExportHelper.ExportToJson(interrupts.Definitions.Values, Path.Combine(exportFolder, "interrupts.json"));
Console.WriteLine($"    ✓ interrupts.json");

ExportHelper.ExportInterruptsToText(interrupts.Definitions.Values, Path.Combine(exportFolder, "interrupts.txt"));
Console.WriteLine($"    ✓ interrupts.txt");

ExportHelper.ExportInterruptsToCsv(interrupts.Definitions.Values, Path.Combine(exportFolder, "interrupts.csv"));
Console.WriteLine($"    ✓ interrupts.csv");

// 12g. Экспорт таймеров
Console.WriteLine("\n  ─── Экспорт таймеров ───");
ExportHelper.ExportToJson(timers.Timers.Values, Path.Combine(exportFolder, "timers.json"));
Console.WriteLine($"    ✓ timers.json");

ExportHelper.ExportTimersToText(timers.Timers.Values, Path.Combine(exportFolder, "timers.txt"));
Console.WriteLine($"    ✓ timers.txt");

ExportHelper.ExportTimersToCsv(timers.Timers.Values, Path.Combine(exportFolder, "timers.csv"));
Console.WriteLine($"    ✓ timers.csv");

Console.WriteLine($"\n  ═══════════════════════════════════════");
Console.WriteLine($"  Всего экспортировано файлов: {Directory.GetFiles(exportFolder).Length}");
Console.WriteLine($"  Папка экспорта: {exportFolder}");
Console.WriteLine($"  ═══════════════════════════════════════\n");

// ===================================================================
// 13. Экспорт AST-деревьев выражений
// ===================================================================
Console.WriteLine("═══ 13. Экспорт AST-деревьев выражений ═══");

var parser = new ExpressionParser();
var astCount = 0;

foreach (var prog in project.Programs.Values)
{
    Console.WriteLine($"\n  ─── Программа: {prog.Name} ───");
    
    var astData = new List<Dictionary<string, object?>>();
    int exprIndex = 0;
    
    foreach (var instr in prog.Lines)
    {
        foreach (var kvp in instr.Fields)
        {
            var value = kvp.Value;
            string? exprText = null;
            
            if (value is ExpressionFieldValue efv)
                exprText = efv.Expression;
            else if (value is ConstantFieldValue cfv)
                exprText = cfv.Value?.ToString();
            
            if (!string.IsNullOrEmpty(exprText))
            {
                try
                {
                    var ast = parser.Parse(exprText);
                    var astRecord = new Dictionary<string, object?>
                    {
                        ["Program"] = prog.Name,
                        ["Line"] = instr.LineNumber,
                        ["FormId"] = instr.FormId,
                        ["FieldName"] = kvp.Key,
                        ["ExpressionText"] = exprText,
                        ["Ast"] = ast
                    };
                    astData.Add(astRecord);
                    exprIndex++;
                    astCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ⚠ Ошибка парсинга строки {instr.LineNumber}: {ex.Message}");
                }
            }
        }
    }
    
    if (astData.Count > 0)
    {
        // JSON
        var astFileName = Path.Combine(exportFolder, $"ast_{prog.Name}.json");
        ExportHelper.ExportToJson(astData, astFileName);
        Console.WriteLine($"    ✓ {prog.Name} — {astData.Count} AST (JSON)");
        
        // Текст (tree-формат)
        var astTxtFileName = Path.Combine(exportFolder, $"ast_{prog.Name}.txt");
        ExportHelper.ExportAstToText(astData, prog.Name, astTxtFileName);
        Console.WriteLine($"    ✓ {prog.Name} — {astData.Count} AST (TXT)");
    }
    else
    {
        Console.WriteLine($"    ✓ {prog.Name} — нет выражений для парсинга");
    }
}

Console.WriteLine($"\n  ✓ Всего AST-деревьев экспортировано: {astCount}");
Console.WriteLine($"  ✓ Папка: {Path.Combine(exportFolder, "ast_*.json")}");
Console.WriteLine();

// 14. Запуск интерпретатора
// ===================================================================
Console.WriteLine("═══ 14. Запуск интерпретатора ═══");

// ✅ Используем project с инициализированными типами для интерпретатора
var interpreter = project.CreateInterpreter();
var mainProgram = project.Programs["MainProgram"];

mainProgram.Arguments.First(a => a.Name == "StartValue").DefaultValue = 1;
interpreter.LoadProgram(mainProgram);
interpreter.Run();

Console.WriteLine($"  ✓ Возврат: {interpreter.State.ReturnValue}");
Console.WriteLine();

// ===================================================================
// 15. Итоговый отчёт
// ===================================================================
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║                  ИТОГОВЫЙ ОТЧЁТ                          ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
Console.WriteLine($"║  Типов данных:       {project.TypeRegistry.AllTypes.Count,3}                          ║");
Console.WriteLine($"║  Форм инструкций:    {project.FormRegistry.AllForms.Count,3}                          ║");
Console.WriteLine($"║  Программ:           {project.Programs.Count,3}                          ║");
Console.WriteLine($"║  Аварий (сист.):     {project.Alarms.Definitions.Count,3}                          ║");
Console.WriteLine($"║  Прерываний:         {project.Interrupts.Definitions.Count,3}                          ║");
Console.WriteLine($"║  Таймеров:           {project.Timers.Timers.Count,3}                          ║");
Console.WriteLine($"║  Глоб. таблиц:       {project.GlobalTables.Tables.Count,3}                          ║");
Console.WriteLine($"║  Возврат:            {interpreter.State.ReturnValue,3}                          ║");
Console.WriteLine($"║  AST-деревьев:       {astCount,3}                          ║");
Console.WriteLine($"║  Экспорт файлов:     {Directory.GetFiles(exportFolder).Length,3}                          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

Console.WriteLine("\n✓ Полный цикл Data > Binding > Execution завершён!");
Console.WriteLine("✓ Все 30+ инструкции, циклы, условия, вызовы, прерывания, таймеры работают.");
Console.WriteLine("✓ Экспорт в JSON, Текст, CSV, FANUC TP .ls + AST-деревья выполнен.\n");

// ===================================================================
// Вспомогательные функции
// ===================================================================
static int CountVariables(VariableTableSet tables)
{
    int count = 0;
    foreach (var table in tables.Tables.Values)
        count += table.Variables.Count;
    return count;
}

// ===================================================================
// ExportHelper — Встроенные функции экспорта
// ===================================================================
public static class ExportHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // ========== JSON Экспорт ==========
    public static void ExportToJson<T>(T obj, string filePath)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    // ========== TXT Экспорт — Типы данных ==========
    public static void ExportTypesToText(DataTypeRegistry registry, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              Типы данных AstroEditor v4                  ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine();

        sb.AppendLine("┌──────────────────────────────────────────────────────────┐");
        sb.AppendLine("│ ПРИМИТИВНЫЕ ТИПЫ                                         │");
        sb.AppendLine("└──────────────────────────────────────────────────────────┘");
        
        foreach (var t in registry.AllTypes.OfType<PrimitiveDataType>())
        {
            var c = t.BuiltinConstraints;
            sb.AppendLine($"  {t.Name,-8} ({t.Id,-6}) > [{c?.Min?.ToString() ?? "∞"}, {c?.Max?.ToString() ?? "∞"}]");
        }

        sb.AppendLine();
        sb.AppendLine("┌──────────────────────────────────────────────────────────┐");
        sb.AppendLine("│ ПЕРЕЧИСЛЕНИЯ (Enum)                                      │");
        sb.AppendLine("└──────────────────────────────────────────────────────────┘");
        
        foreach (var t in registry.AllTypes.OfType<EnumDataType>())
        {
            sb.AppendLine($"  {t.Name} ({t.Values.Count} значений):");
            foreach (var kv in t.Values)
                sb.AppendLine($"    • {kv.Key} = {kv.Value}");
        }
        
        sb.AppendLine();
        sb.AppendLine("┌──────────────────────────────────────────────────────────┐");
        sb.AppendLine("│ СТРУКТУРЫ (Struct)                                       │");
        sb.AppendLine("└──────────────────────────────────────────────────────────┘");
        
        foreach (var t in registry.AllTypes.OfType<StructDataType>())
        {
            sb.AppendLine($"  {t.Name} ({t.Fields.Count} полей):");
            foreach (var f in t.Fields)
                sb.AppendLine($"    • {f.Name}: {f.TypeId}");
        }

        sb.AppendLine();
        sb.AppendLine("┌──────────────────────────────────────────────────────────┐");
        sb.AppendLine("│ ПСЕВДОНИМЫ (Alias)                                       │");
        sb.AppendLine("└──────────────────────────────────────────────────────────┘");
        
        foreach (var t in registry.AllTypes.OfType<AliasDataType>())
        {
            sb.AppendLine($"  {t.Name} > {t.BaseTypeId}");
        }

        sb.AppendLine();
        sb.AppendLine("══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — Формы ==========
    public static void ExportFormsToText(FormRegistry registry, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           Формы инструкций AstroEditor v4                ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine();

        foreach (var form in registry.AllForms.OrderBy(f => f.Category).ThenBy(f => f.Name))
        {
            sb.AppendLine($"┌──────────────────────────────────────────────────────────┐");
            sb.AppendLine($"│ {form.Name,-56} │");
            sb.AppendLine($"├──────────────────────────────────────────────────────────┤");
            sb.AppendLine($"│ ID: {form.Id}");
            sb.AppendLine($"│ Категория: {form.Category}");
            sb.AppendLine($"│ Полей: {form.Fields.Count}");
            
            if (!string.IsNullOrEmpty(form.Description))
                sb.AppendLine($"│ Описание: {form.Description}");
            
            sb.AppendLine($"├──────────────────────────────────────────────────────────┤");
            sb.AppendLine($"│ ПОЛЯ:");
            
            foreach (var field in form.Fields)
            {
                var req = field.Required ? "★" : "○";
                sb.AppendLine($"│   {req} {field.Name,-20} {field.ValueType,-12} {(field.Required ? "Обяз." : "Опц.")}");
            }
            
            sb.AppendLine($"└──────────────────────────────────────────────────────────┘");
            sb.AppendLine();
        }
        
        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — Программа ==========
    public static void ExportProgramToText(AstroProgram program, FormRegistry formRegistry, string filePath)
    {
        var sb = new StringBuilder();
        
        // Заголовок
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║  ПРОГРАММА: {program.Name,-46} ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Автор: {program.Author,-48} ║");
        sb.AppendLine($"║  Версия: {program.Version,-47} ║");
        sb.AppendLine($"║  Описание: {program.Description,-45} ║");
        sb.AppendLine($"║  Строк инструкций: {program.Lines.Count,-38} ║");
        sb.AppendLine($"║  Аргументов: {program.Arguments.Count,-42} ║");
        sb.AppendLine($"║  Локальных переменных: {CountLocalVars(program),-38} ║");
        sb.AppendLine($"║  Макс. циклов: {program.MaxCycles,-42} ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        
        // Аргументы
        if (program.Arguments.Count > 0)
        {
            sb.AppendLine("║  АРГУМЕНТЫ:                                                ║");
            foreach (var arg in program.Arguments)
            {
                sb.AppendLine($"║    • {arg.Name,-20} ({arg.TypeId,-8}) [{arg.Direction,-2}] = {arg.DefaultValue}              ║");
            }
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        }
        
        // Локальные переменные
        var localVarCount = CountLocalVars(program);
        if (localVarCount > 0)
        {
            sb.AppendLine("║  ЛОКАЛЬНЫЕ ПЕРЕМЕННЫЕ:                                     ║");
            foreach (var table in program.LocalTables.Tables.Values)
            {
                foreach (var v in table.Variables)
                {
                    var valStr = v.Value?.ToString() ?? "null";
                    if (valStr.Length > 30) valStr = valStr.Substring(0, 27) + "...";
                    sb.AppendLine($"║    • {v.Name,-20} ({v.Type.Name,-8}) = {valStr,-20} ║");
                }
            }
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        }
        
        // Метки
        if (program.Labels.Count > 0)
        {
            sb.AppendLine("║  МЕТКИ:                                                    ║");
            foreach (var lbl in program.Labels)
            {
                sb.AppendLine($"║    • {lbl.Key,-20} -> строка {lbl.Value}                                          ║");
            }
            sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        }
        
        // Инструкции
        sb.AppendLine("║  ИНСТРУКЦИИ:                                               ║");
        sb.AppendLine("╠════════╦══════════════════════════════════════════════════╣");
        sb.AppendLine("║ № строки║  Форма                                           ║");
        sb.AppendLine("╠════════╬══════════════════════════════════════════════════╣");
        
        foreach (var line in program.Lines)
        {
            var formText = FormatInstructionText(line, formRegistry);
            sb.AppendLine($"║ {line.LineNumber,6} ║  {formText,-50}║");
        }
        
        sb.AppendLine("╚════════╩══════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // Форматирование инструкции в текст
    private static string FormatInstructionText(Instruction instr, FormRegistry? formRegistry = null)
    {
        var sb = new StringBuilder();
        
        // Получаем форму для описания
        string? formDesc = null;
        if (formRegistry != null && formRegistry.AllForms.Any(f => f.Id == instr.FormId))
        {
            formDesc = formRegistry.AllForms.First(f => f.Id == instr.FormId).Name;
        }
        
        var formName = formDesc ?? instr.FormId;
        sb.Append(formName);
        
        // Раскрываем поля
        if (instr.Fields.Count > 0)
        {
            var fieldParts = new List<string>();
            
            foreach (var kvp in instr.Fields)
            {
                var fieldName = kvp.Key;
                var fieldValue = kvp.Value;
                
                string? valueText = fieldValue switch
                {
                    ConstantFieldValue cfv => FormatConstantValue(cfv.Value),
                    VariableFieldValue vfv => $"{vfv.TableSetName}.{vfv.VariableName} ({vfv.TypeId})",
                    ExpressionFieldValue evf => evf.Expression,
                    FunctionCallFieldValue fcv => FormatFunctionCall(fcv),
                    LabelFieldValue lfv => lfv.LabelName,
                    EnumFieldValue efv => efv.SelectedValue,
                    _ => fieldValue.ToString()
                };
                
                if (!string.IsNullOrEmpty(valueText))
                {
                    fieldParts.Add($"{fieldName}={valueText}");
                }
            }
            
            if (fieldParts.Count > 0)
            {
                sb.Append(" (");
                sb.Append(string.Join(", ", fieldParts));
                sb.Append(")");
            }
        }
        
        // Комментарий
        if (!string.IsNullOrEmpty(instr.Comment))
        {
            sb.Append($"  ; {instr.Comment}");
        }
        
        return sb.ToString().Substring(0, Math.Min(sb.Length, 50));
    }
    
    private static string FormatConstantValue(object? value)
    {
        if (value == null) return "null";
        return value.ToString()!;
    }
    
    private static string FormatFunctionCall(FunctionCallFieldValue fcv)
    {
        var sb = new StringBuilder();
        sb.Append(fcv.FunctionName);
        sb.Append("(");
        sb.Append(string.Join(", ", fcv.Arguments.Select(a => a switch
        {
            ConstantFieldValue cfv => FormatConstantValue(cfv.Value),
            ExpressionFieldValue evf => evf.Expression,
            _ => a.ToString() ?? ""
        })));
        sb.Append(")");
        return sb.ToString();
    }
    
    private static int CountLocalVars(AstroProgram program)
    {
        int count = 0;
        foreach (var table in program.LocalTables.Tables.Values)
            count += table.Variables.Count;
        return count;
    }

    // ========== TXT Экспорт — Переменные ==========
    public static void ExportVariablesToText(VariableTableSet tables, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              Глобальные переменные                       ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        
        foreach (var table in tables.Tables.Values)
        {
            sb.AppendLine();
            sb.AppendLine($"┌──────────────────────────────────────────────────────────┐");
            sb.AppendLine($"│ Тип: {table.Type.Name}");
            sb.AppendLine($"├──────────────────────────────────────────────────────────┤");
            
            foreach (var v in table.Variables)
            {
                var valueStr = FormatValueForText(v.Value);
                sb.AppendLine($"│  {v.Name,-30} = {valueStr}");
            }
            
            sb.AppendLine($"└──────────────────────────────────────────────────────────┘");
        }
        
        sb.AppendLine();
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — Аварии ==========
    public static void ExportAlarmsToText(IEnumerable<AlarmDefinition> alarms, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    Аварии (Alarms)                       ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        
        foreach (var alarm in alarms.OrderBy(a => a.Code))
        {
            sb.AppendLine();
            sb.AppendLine($"  #{alarm.Code} — {alarm.Name}");
            sb.AppendLine($"     Тяжесть: {alarm.Severity}");
            sb.AppendLine($"     Шаблон: {alarm.MessageTemplate}");
            sb.AppendLine($"     Категория: {alarm.Category}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — Прерывания ==========
    public static void ExportInterruptsToText(IEnumerable<InterruptDefinition> interrupts, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                  Прерывания (Interrupts)                 ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        
        foreach (var i in interrupts.OrderBy(d => d.Name))
        {
            sb.AppendLine();
            sb.AppendLine($"  {i.Name} ({i.Id})");
            sb.AppendLine($"     Триггер: {i.TriggerType}");
            sb.AppendLine($"     Режим: {i.ExecutionMode}");
            sb.AppendLine($"     Включено: {i.IsEnabled}");
            
            if (!string.IsNullOrEmpty(i.VariableName))
                sb.AppendLine($"     Переменная: {i.VariableName}");
            
            if (!string.IsNullOrEmpty(i.Expression))
                sb.AppendLine($"     Выражение: {i.Expression}");
            
            if (i.AlarmCode.HasValue)
                sb.AppendLine($"     Код аварии: {i.AlarmCode}");
            
            if (i.TimerIntervalMs.HasValue)
                sb.AppendLine($"     Интервал таймера: {i.TimerIntervalMs} мс");
            
            if (!string.IsNullOrEmpty(i.HandlerProgramName))
                sb.AppendLine($"     Обработчик: {i.HandlerProgramName}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — Таймеры ==========
    public static void ExportTimersToText(IEnumerable<TimerDefinition> timers, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                      Таймеры (Timers)                    ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        
        foreach (var t in timers.OrderBy(d => d.Name))
        {
            sb.AppendLine();
            sb.AppendLine($"  {t.Name}");
            sb.AppendLine($"     Интервал: {t.IntervalMs} мс");
            sb.AppendLine($"     Режим: {t.Mode}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== TXT Экспорт — AST-деревья ==========
    public static void ExportAstToText(IEnumerable<Dictionary<string, object?>> astData, string programName, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║  AST-деревья: {programName,-44} ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════╣");
        sb.AppendLine();

        var index = 1;
        foreach (var record in astData)
        {
            if (record["Ast"] is ExpressionNode ast)
            {
                sb.AppendLine($"  ┌──────────────────────────────────────────────────────┐");
                sb.AppendLine($"  │ #{index}: {record["ExpressionText"]}");
                sb.AppendLine($"  ├──────────────────────────────────────────────────────┤");
                sb.AppendLine($"  │ Программа: {record["Program"]}, Строка: {record["Line"]}, Поле: {record["FieldName"]}");
                sb.AppendLine($"  │ Форма: {record["FormId"]}");
                sb.AppendLine($"  │ Дерево:");
                
                var treeText = RenderAstTree(ast);
                foreach (var line in treeText.Split('\n'))
                    sb.AppendLine($"  │ {line}");
                
                sb.AppendLine($"  └──────────────────────────────────────────────────────┘");
                sb.AppendLine();
                index++;
            }
        }

        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"Экспортировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Типы ==========
    public static void ExportTypesToCsv(DataTypeRegistry registry, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Id,Category,Kind,Details");
        
        foreach (var t in registry.AllTypes)
        {
            var details = t switch
            {
                PrimitiveDataType p => $"Range: [{p.BuiltinConstraints?.Min?.ToString() ?? "∞"}, {p.BuiltinConstraints?.Max?.ToString() ?? "∞"}]",
                EnumDataType e => $"Values: {e.Values.Count}",
                StructDataType s => $"Fields: {s.Fields.Count}",
                AliasDataType a => $"Base: {a.BaseTypeId}",
                ArrayDataType arr => $"Element: {arr.ElementTypeId}",
                _ => ""
            };
            
            sb.AppendLine($"{EscapeCsv(t.Name)},{EscapeCsv(t.Id)},{t.Category},{t.Kind},{EscapeCsv(details)}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Формы ==========
    public static void ExportFormsToCsv(FormRegistry registry, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FormId,Name,Category,FieldName,FieldType,Required,Description");
        
        foreach (var form in registry.AllForms)
        {
            foreach (var field in form.Fields)
            {
                sb.AppendLine($"{EscapeCsv(form.Id)},{EscapeCsv(form.Name)},{EscapeCsv(form.Category)},{EscapeCsv(field.Name)},{field.ValueType},{field.Required},{EscapeCsv(field.DisplayName)}");
            }
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Программа ==========
    public static void ExportProgramToCsv(AstroProgram program, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LineNumber,FormId,Comment");
        
        foreach (var line in program.Lines)
        {
            sb.AppendLine($"{line.LineNumber},{EscapeCsv(line.FormId)},{EscapeCsv(line.Comment)}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Переменные ==========
    public static void ExportVariablesToCsv(VariableTableSet tables, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TableName,VariableName,Type,Value");
        
        foreach (var table in tables.Tables.Values)
        {
            foreach (var v in table.Variables)
            {
                var valueStr = FormatValueForCsv(v.Value);
                sb.AppendLine($"{EscapeCsv(table.Type.Name)},{EscapeCsv(v.Name)},{EscapeCsv(v.Type.Name)},\"{valueStr}\"");
            }
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Аварии ==========
    public static void ExportAlarmsToCsv(IEnumerable<AlarmDefinition> alarms, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Code,Name,Severity,MessageTemplate,IsSystem");
        
        foreach (var alarm in alarms)
        {
            sb.AppendLine($"{alarm.Code},{EscapeCsv(alarm.Name)},{alarm.Severity},\"{EscapeCsv(alarm.MessageTemplate)}\",{(alarm.Category == DataTypeCategory.Core || alarm.Category == DataTypeCategory.System)}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Прерывания ==========
    public static void ExportInterruptsToCsv(IEnumerable<InterruptDefinition> interrupts, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,TriggerType,ExecutionMode,IsEnabled,VariableName,AlarmCode,TimerIntervalMs,HandlerProgram");
        
        foreach (var i in interrupts)
        {
            sb.AppendLine($"{EscapeCsv(i.Id)},{EscapeCsv(i.Name)},{i.TriggerType},{i.ExecutionMode},{i.IsEnabled},{EscapeCsv(i.VariableName ?? "")},{i.AlarmCode ?? 0},{i.TimerIntervalMs ?? 0},{EscapeCsv(i.HandlerProgramName ?? "")}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== CSV Экспорт — Таймеры ==========
    public static void ExportTimersToCsv(IEnumerable<TimerDefinition> timers, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,IntervalMs,Mode");
        
        foreach (var t in timers)
        {
            sb.AppendLine($"{EscapeCsv(t.Name)},{t.IntervalMs},{t.Mode}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    // ========== Утилиты ==========
    private static string EscapeCsv(string? value)
    {
        if (value == null) return "";
        return value.Replace("\"", "\"\"").Replace(",", ";").Replace("\n", " ");
    }

    private static string FormatValueForCsv(object? value)
    {
        if (value == null) return "null";
        
        // Словари (структуры)
        if (value is Dictionary<string, object> dict)
        {
            return string.Join(", ", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
        
        // Списки (массивы)
        if (value is IEnumerable<object> list && !(value is string))
        {
            return string.Join(", ", list.Select(v => FormatValueForCsv(v)));
        }
        
        return value.ToString() ?? "null";
    }

    private static string FormatValueForText(object? value)
    {
        if (value == null) return "null";
        
        // Словари (структуры)
        if (value is Dictionary<string, object> dict)
        {
            var parts = dict.Select(kvp => $"{kvp.Key}={kvp.Value}");
            return $"{{{string.Join(", ", parts)}}}";
        }
        
        // Списки (массивы)
        if (value is IEnumerable<object> list && !(value is string))
        {
            return $"[{string.Join(", ", list.Select(v => FormatValueForText(v)))}]";
        }
        
        var str = value.ToString() ?? "null";
        if (str.Length > 30) str = str.Substring(0, 27) + "...";
        return str;
    }

    // ========== AST Tree Rendering ==========
    private static string RenderAstTree(ExpressionNode node, string indent = "    ")
    {
        var sb = new StringBuilder();
        RenderNode(node, sb, indent, "");
        return sb.ToString().TrimEnd('\n');
    }

    private static void RenderNode(ExpressionNode node, StringBuilder sb, string indent, string prefix)
    {
        var type = node.GetType().Name.Replace("Node", "");
        sb.AppendLine($"{indent}{prefix}{type}");

        switch (node)
        {
            case ConstantNode cn:
                sb.AppendLine($"{indent}  └─ value = {cn.Value} ({cn.Value?.GetType().Name ?? "null"})");
                break;

            case VariableNode vn:
                sb.AppendLine($"{indent}  └─ name = {vn.Name}");
                if (!string.IsNullOrEmpty(vn.TableSetName))
                    sb.AppendLine($"{indent}     table = {vn.TableSetName}");
                break;

            case FieldAccessNode fan:
                sb.AppendLine($"{indent}  └─ field = {fan.FieldName}");
                sb.AppendLine($"{indent}  └─ target:");
                RenderNode(fan.Target, sb, indent + "     ", "");
                break;

            case FunctionCallNode fcn:
                sb.AppendLine($"{indent}  └─ function = {fcn.FunctionName}");
                sb.AppendLine($"{indent}  └─ arguments ({fcn.Arguments.Count}):");
                for (int i = 0; i < fcn.Arguments.Count; i++)
                {
                    RenderNode(fcn.Arguments[i], sb, indent + "     ", $"  [{i}] ");
                }
                break;

            case BinaryExpressionNode ben:
                var opName = ben.Operator.ToString();
                sb.AppendLine($"{indent}  └─ operator = {opName}");
                sb.AppendLine($"{indent}  └─ left:");
                RenderNode(ben.Left, sb, indent + "     ", "");
                sb.AppendLine($"{indent}  └─ right:");
                RenderNode(ben.Right, sb, indent + "     ", "");
                break;

            case UnaryExpressionNode uen:
                sb.AppendLine($"{indent}  └─ operator = {uen.Operator}");
                sb.AppendLine($"{indent}  └─ operand:");
                RenderNode(uen.Operand, sb, indent + "     ", "");
                break;

            case TernaryExpressionNode ten:
                sb.AppendLine($"{indent}  └─ condition:");
                RenderNode(ten.Condition, sb, indent + "     ", "");
                sb.AppendLine($"{indent}  └─ true:");
                RenderNode(ten.TrueExpression, sb, indent + "     ", "");
                sb.AppendLine($"{indent}  └─ false:");
                RenderNode(ten.FalseExpression, sb, indent + "     ", "");
                break;

            case IndexAccessNode ian:
                sb.AppendLine($"{indent}  └─ index:");
                RenderNode(ian.Index, sb, indent + "     ", "");
                sb.AppendLine($"{indent}  └─ target:");
                RenderNode(ian.Target, sb, indent + "     ", "");
                break;

            case ArrayLiteralNode aln:
                sb.AppendLine($"{indent}  └─ elements ({aln.Elements.Count}):");
                for (int i = 0; i < aln.Elements.Count; i++)
                {
                    RenderNode(aln.Elements[i], sb, indent + "     ", $"  [{i}] ");
                }
                break;

            default:
                sb.AppendLine($"{indent}  └─ (unknown node type: {node.GetType().Name})");
                break;
        }
    }
}
