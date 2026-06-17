using AstroEditor.Core.v4.Common;
using AstroEditor.Core.v4.Types;
using AstroEditor.Core.v4.Variables;
using AstroEditor.Core.v4.Tables;
using AstroEditor.Core.v4.Programs;
using AstroEditor.Core.v4.Forms;
using AstroEditor.Core.v4.Expressions;
using AstroEditor.Core.v4.Interpreter;
using AstroEditor.Core.v4.Serialization;

// 1. Создаём реестры
var typeRegistry = new DataTypeRegistry();
var formRegistry = new FormRegistry();

// 2. Регистрируем примитивы
var intType = PrimitiveDataType.Int(); intType.Id = "int"; typeRegistry.RegisterType(intType);
var doubleType = PrimitiveDataType.Double(); doubleType.Id = "double"; typeRegistry.RegisterType(doubleType);
var boolType = PrimitiveDataType.Bool(); boolType.Id = "bool"; typeRegistry.RegisterType(boolType);
var stringType = PrimitiveDataType.String(); stringType.Id = "string"; typeRegistry.RegisterType(stringType);
var realType = new AliasDataType { Name = "REAL", Category = DataTypeCategory.System, BaseTypeId = "double" };
typeRegistry.RegisterType(realType);

// Структура POSITION
var posType = new StructDataType
{
    Name = "POSITION",
    Category = DataTypeCategory.System,
    Fields = new List<StructField>
    {
        new StructField { Name = "X", TypeId = "double" },
        new StructField { Name = "Y", TypeId = "double" },
        new StructField { Name = "Z", TypeId = "double" }
    }
};
typeRegistry.RegisterType(posType);
typeRegistry.ResolveReferences();

// 3. Регистрируем все формы (включая новые)
formRegistry.RegisterForm(BuiltinForms.CreateAssignmentForm());
formRegistry.RegisterForm(BuiltinForms.CreateWhileForm());
formRegistry.RegisterForm(BuiltinForms.CreateCallForm());
formRegistry.RegisterForm(BuiltinForms.CreateLabelForm());
formRegistry.RegisterForm(BuiltinForms.CreateJumpLblForm());
formRegistry.RegisterForm(BuiltinForms.CreateJumpIfForm());
formRegistry.RegisterForm(BuiltinForms.CreateReturnForm());
formRegistry.RegisterForm(BuiltinForms.CreateBreakForm());
formRegistry.RegisterForm(BuiltinForms.CreateContinueForm());
formRegistry.RegisterForm(BuiltinForms.CreateIfForm());
formRegistry.RegisterForm(BuiltinForms.CreateElseForm());
formRegistry.RegisterForm(BuiltinForms.CreateEndIfForm());
formRegistry.RegisterForm(BuiltinForms.CreateSwitchForm());
formRegistry.RegisterForm(BuiltinForms.CreateCaseForm());
formRegistry.RegisterForm(BuiltinForms.CreateDefaultForm());
formRegistry.RegisterForm(BuiltinForms.CreateEndSwitchForm());
formRegistry.RegisterForm(BuiltinForms.CreateForForm());
formRegistry.RegisterForm(BuiltinForms.CreateEndForForm());

// 4. Глобальные таблицы
var globalSet = new VariableTableSet { Name = "GlobalVariables", IsGlobal = true };
globalSet.GetOrCreateTable(intType).AddVariable(new Variable("GlobalCounter", intType, 0));
globalSet.GetOrCreateTable(doubleType).AddVariable(new Variable("Pi", doubleType, 3.14159));
globalSet.GetOrCreateTable(boolType).AddVariable(new Variable("Flag", boolType, false));
globalSet.GetOrCreateTable(stringType).AddVariable(new Variable("Message", stringType, "Hello"));

// 5. Создаём программу с тестовыми конструкциями
var program = new AstroProgram
{
    Name = "TestProgram",
    Author = "Tester",
    Description = "Тестовая программа, проверяющая все конструкции",
    Version = "1.0",
    ReturnTypeId = "int",
    IsMenuFunction = false,
    IsBackground = false,
    TaskGroup = 1,
    MaxCycles = 1000,
    Permissions = new ProgramPermissions
    {
        ReadRoles = new List<string> { "operator" },
        WriteRoles = new List<string> { "programmer" },
        ExecuteRoles = new List<string> { "operator", "programmer" }
    }
};

// Аргументы
program.Arguments.Add(new Argument { Name = "StartValue", TypeId = "int", Direction = ArgumentDirection.In, DefaultValue = 0 });
program.Arguments.Add(new Argument { Name = "Multiplier", TypeId = "real", Direction = ArgumentDirection.In, DefaultValue = 2.0 });

// Локальные переменные
program.AddLocalVariable(new Variable("Counter", intType, 0), typeRegistry);
program.AddLocalVariable(new Variable("Sum", intType, 0), typeRegistry);
program.AddLocalVariable(new Variable("Temp", realType, 0.0), typeRegistry);
program.AddLocalVariable(new Variable("IsEven", boolType, false), typeRegistry);

// Формируем инструкции
int line = 1;

// 1. Присваивание: Counter = StartValue
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
        ["expression"] = new ExpressionFieldValue("StartValue")
    },
    Comment = "Инициализация счётчика"
});

// 2. Присваивание: Sum = 0
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
        ["expression"] = new ConstantFieldValue(0)
    },
    Comment = "Обнуление суммы"
});

// 3. Метка START
program.Lines.Add(new Instruction(line++, "core.lbl")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["labelName"] = new ConstantFieldValue("START")
    }
});

// 4. Проверка условия выхода: IF Counter >= 10 THEN GOTO END
program.Lines.Add(new Instruction(line++, "core.jumpif")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["condition"] = new ExpressionFieldValue("Counter >= 10"),
        ["labelName"] = new ConstantFieldValue("END")
    },
    Comment = "Выход при достижении 10"
});

// 5. Тело цикла: Sum = Sum + Counter
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
        ["expression"] = new ExpressionFieldValue("Sum + Counter")
    }
});

// 6. IF (Counter % 2 == 0) THEN Set IsEven = true ELSE false
program.Lines.Add(new Instruction(line++, "core.if")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["condition"] = new ExpressionFieldValue("(Counter % 2) == 0")
    }
});
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"),
        ["expression"] = new ConstantFieldValue(true)
    }
});
program.Lines.Add(new Instruction(line++, "core.else"));
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "IsEven", "bool"),
        ["expression"] = new ConstantFieldValue(false)
    }
});
program.Lines.Add(new Instruction(line++, "core.endif"));

// 7. Switch по Counter
program.Lines.Add(new Instruction(line++, "core.switch")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["expression"] = new ExpressionFieldValue("Counter")
    }
});
program.Lines.Add(new Instruction(line++, "core.case")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["value"] = new ConstantFieldValue(2)
    }
});
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"),
        ["expression"] = new ConstantFieldValue(100.0)
    }
});
program.Lines.Add(new Instruction(line++, "core.default"));
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Temp", "real"),
        ["expression"] = new ConstantFieldValue(0.0)
    }
});
program.Lines.Add(new Instruction(line++, "core.endswitch"));

// 8. FOR цикл: for i from 1 to 5 step 1
program.Lines.Add(new Instruction(line++, "core.for")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
        ["start"] = new ExpressionFieldValue("Counter + 1"),
        ["end"] = new ExpressionFieldValue("Counter + 5"),
        ["step"] = new ExpressionFieldValue("1")
    },
    Comment = "Внутренний FOR"
});
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Sum", "int"),
        ["expression"] = new ExpressionFieldValue("Sum + Counter")
    }
});
program.Lines.Add(new Instruction(line++, "core.endfor"));

// 9. Инкремент счётчика
program.Lines.Add(new Instruction(line++, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
        ["expression"] = new ExpressionFieldValue("Counter + 1")
    }
});

// 10. Переход на START
program.Lines.Add(new Instruction(line++, "core.jumplbl")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["labelName"] = new ConstantFieldValue("START")
    }
});

// 11. Метка END
program.Lines.Add(new Instruction(line++, "core.lbl")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["labelName"] = new ConstantFieldValue("END")
    }
});

// 12. Return Sum
program.Lines.Add(new Instruction(line++, "core.return")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["value"] = new ExpressionFieldValue("Sum")
    }
});

// Заполняем метки
program.Labels["START"] = 3;
program.Labels["END"] = 22;

// 6. Сохраняем всё в папку
string baseFolder = Path.Combine(Environment.CurrentDirectory, "AstroData");
Directory.CreateDirectory(baseFolder);

// Сохраняем типы, формы, глобальные таблицы
AstroSerializer.SaveDataTypeRegistry(typeRegistry, Path.Combine(baseFolder, "Registry"));
AstroSerializer.SaveFormRegistry(formRegistry, Path.Combine(baseFolder, "Registry"));
AstroSerializer.SaveGlobalTables(globalSet, Path.Combine(baseFolder, "Registry"));

// Сохраняем программу в JSON
var programFolder = Path.Combine(baseFolder, "Programs");
AstroSerializer.SaveProgram(program, programFolder);

// Сохраняем текстовое представление
var textPath = Path.Combine(programFolder, $"{program.Name}.txt");
// ProgramTextGenerator.SaveToFile(program, textPath);


try
{
    Console.WriteLine($"Saving program to: {Path.Combine(programFolder, $"{program.Name}.ast")}");
    Console.WriteLine($"Saving text to: {textPath}");
    AstroSerializer.SaveProgram(program, programFolder);
    ProgramTextGenerator.SaveToFile(program, textPath);
    Console.WriteLine($"Данные сохранены в {baseFolder}");
    Console.WriteLine($"Program file exists: {File.Exists(Path.Combine(programFolder, $"{program.Name}.ast"))}");
    Console.WriteLine($"Text file exists: {File.Exists(textPath)}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error saving files: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}



// 7. Загружаем обратно
var loadedTypes = AstroSerializer.LoadDataTypeRegistry(Path.Combine(baseFolder, "Registry"));
var loadedForms = AstroSerializer.LoadFormRegistry(Path.Combine(baseFolder, "Registry"));
var loadedGlobals = AstroSerializer.LoadGlobalTables(Path.Combine(baseFolder, "Registry"), loadedTypes);
var loadedProgram = AstroSerializer.LoadProgram(programFolder, program.Name, loadedTypes);

Console.WriteLine("Данные успешно загружены.");

// 8. Подготовка интерпретатора
var interpreterContext = new InterpreterContext
{
    TypeRegistry = loadedTypes,
    FormRegistry = loadedForms,
    GlobalTables = loadedGlobals,
    Functions = BuiltinFunctions.GetFunctions(),
    ProgramRegistry = new Dictionary<string, AstroProgram>
    {
        [loadedProgram.Name] = loadedProgram
    }
};

var interpreter = new AstroInterpreter(interpreterContext);

// Подписываемся на события (для отладки)
interpreterContext.OnBeforeInstruction += (state, instr) =>
{
    Console.WriteLine($"Выполняется строка {instr.LineNumber}: {instr.FormId}");
};
interpreterContext.OnError += (state, ex) =>
{
    Console.WriteLine($"Ошибка: {ex.Message}");
};

// Загружаем программу
interpreter.LoadProgram(loadedProgram); // метод есть в AstroInterpreter

// Задаём начальные значения аргументов (в локальных таблицах)
var startVar = loadedProgram.LocalTables.FindVariable("StartValue");
if (startVar != null) startVar.Value = 2;
var multVar = loadedProgram.LocalTables.FindVariable("Multiplier");
if (multVar != null) multVar.Value = 3.0;

Console.WriteLine("\n=== Запуск программы ===");
interpreter.Run(); // метод есть в AstroInterpreter

Console.WriteLine($"\nРезультат выполнения: {interpreter.State.ReturnValue}");
Console.WriteLine("Значения локальных переменных после выполнения:");
foreach (var kv in loadedProgram.LocalTables.Tables)
{
    foreach (var v in kv.Value.Variables)
        Console.WriteLine($"  {v.Name} = {v.Value}");
}

Console.WriteLine("Глобальные переменные:");
foreach (var kv in loadedGlobals.Tables)
{
    foreach (var v in kv.Value.Variables)
        Console.WriteLine($"  {v.Name} = {v.Value}");
}