# 📘 AstroEditor v4 — Полная документация по ядру

## 📖 Оглавление

1. [Общая архитектура](#1-общая-архитектура)
2. [Типы данных](#2-типы-данных)
3. [Переменные и таблицы](#3-переменные-и-таблицы)
4. [Привязки (Binding)](#4-привязки-binding)
5. [Программы и инструкции](#5-программы-и-инструкции)
6. [Формы инструкций](#6-формы-инструкций)
7. [Выражения и функции](#7-выражения-и-функции)
8. [Интерпретатор](#8-интерпретатор)
9. [Обработка исключений](#9-обработка-исключений)
10. [Аварии (Alarms)](#10-аварии-alarms)
11. [Прерывания (Interrupts)](#11-прерывания-interrupts)
12. [Таймеры](#12-таймеры)
13. [Планировщик задач](#13-планировщик-задач)
14. [Система плагинов](#14-система-плагинов)
15. [Сериализация и хранение](#15-сериализация-и-хранение)

---

## 1. Общая архитектура

### 1.1. Слои системы

```
┌─────────────────────────────────────────────────────────┐
│                    UI / Editor Layer                     │
│  (Формы, визуальный редактор, отладчик)                 │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                     Data Layer                           │
│  • ProjectManager — центральный менеджер                │
│  • DataTypeRegistry — реестр типов                      │
│  • FormRegistry — реестр форм                           │
│  • VariableTableSet — таблицы переменных                │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                   Binding Layer                          │
│  • BindingManager — управление привязками               │
│  • BindingRouter — маршрутизация изменений              │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                  Execution Layer                         │
│  • AstroInterpreter — интерпретатор                     │
│  • TaskScheduler — планировщик задач                    │
│  • InterruptManager — прерывания                        │
│  • TimerManager — таймеры                               │
│  • AlarmManager — аварии                                │
└─────────────────────────────────────────────────────────┘
```

### 1.2. Поток выполнения

```
ProjectManager (инициализация)
    ↓
Создание типов, форм, переменных
    ↓
BindingManager (настройка привязок)
    ↓
Создание программ (AstroProgram)
    ↓
TaskScheduler (запуск задач)
    ↓
AstroInterpreter (выполнение инструкций)
    ↓
InterruptManager / TimerManager (события)
    ↓
AlarmManager (аварии)
```

### 1.3. Основные пространства имен

| Namespace | Описание |
|-----------|----------|
| `AstroEditor.Core.Data` | Управление проектом |
| `AstroEditor.Core.Types` | Типы данных |
| `AstroEditor.Core.Variables` | Переменные |
| `AstroEditor.Core.Binding` | Привязки |
| `AstroEditor.Core.Programs` | Программы и инструкции |
| `AstroEditor.Core.Forms` | Формы инструкций |
| `AstroEditor.Core.Expressions` | Выражения и функции |
| `AstroEditor.Core.Interpreter` | Интерпретатор |
| `AstroEditor.Core.Execution` | Прерывания, таймеры |
| `AstroEditor.Core.Alarms` | Аварии |
| `AstroEditor.Core.Plugins` | Плагины |

---

## 2. Типы данных

### 2.1. Категории типов

```csharp
public enum DataTypeCategory
{
    Core,    // Базовые типы системы
    System,  // Системные типы
    User     // Пользовательские типы
}
```

### 2.2. Примитивные типы (PrimitiveDataType)

| Тип | Идентификатор | Диапазон | Описание |
|-----|---------------|----------|----------|
| **SBYTE** | `sbyte` | -128..127 | 8-битное знаковое |
| **BYTE** | `byte` | 0..255 | 8-битное беззнаковое |
| **SHORT** | `short` | -32768..32767 | 16-битное знаковое |
| **USHORT** | `ushort` | 0..65535 | 16-битное беззнаковое |
| **INT** | `int` | -2^31..2^31-1 | 32-битное знаковое |
| **UINT** | `uint` | 0..2^32-1 | 32-битное беззнаковое |
| **LONG** | `long` | -2^63..2^63-1 | 64-битное знаковое |
| **ULONG** | `ulong` | 0..2^64-1 | 64-битное беззнаковое |
| **FLOAT** | `float` | ±3.4×10^38 | 32-битное с плавающей точкой |
| **DOUBLE** | `double` | ±1.7×10^308 | 64-битное с плавающей точкой |
| **DECIMAL** | `decimal` | ±7.9×10^28 | 128-битное десятичное |
| **BOOL** | `bool` | true/false | Логический |
| **CHAR** | `char` | 0..65535 | Символ Unicode |
| **STRING** | `string` | ∞ | Строка |

**Пример использования:**
```csharp
var intType = project.TypeRegistry.GetTypeById("int");
var realType = project.TypeRegistry.GetTypeById("real"); // псевдоним double
```

---

### 2.3. Перечисления (EnumDataType)

**Создание:**
```csharp
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
```

**Структура:**
```csharp
public class EnumDataType : DataType
{
    public Dictionary<string, long> Values { get; set; }
}
```

---

### 2.4. Структуры (StructDataType)

**Создание:**
```csharp
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
```

**Структура поля:**
```csharp
public class StructField
{
    public string Name { get; set; }      // Имя поля
    public string TypeId { get; set; }    // Ссылка на тип
    public object? DefaultValue { get; set; }
}
```

---

### 2.5. Псевдонимы (AliasDataType)

**Создание:**
```csharp
var speedType = new AliasDataType
{
    Id = "speed",
    Name = "SPEED",
    Category = DataTypeCategory.User,
    BaseTypeId = "int"  // Базовый тип
};
```

---

### 2.6. Реестр типов (DataTypeRegistry)

**Методы:**
```csharp
public class DataTypeRegistry
{
    void RegisterType(DataType type);
    DataType? GetTypeById(string id);
    DataType? GetTypeByName(string name);
    IEnumerable<DataType> AllTypes { get; }
    void ResolveReferences();  // Разрешение ссылок между типами
}
```

---

## 3. Переменные и таблицы

### 3.1. Переменная (Variable)

```csharp
public class Variable
{
    public string Name { get; set; }
    public DataType Type { get; set; }
    public object? Value { get; set; }
    
    // Для структур
    public Dictionary<string, object>? Fields { get; set; }
}
```

**Создание:**
```csharp
var intType = project.TypeRegistry.GetTypeById("int");
var counter = new Variable("Counter", intType, 0);
var position = new Variable("Pos", pointType, new Dictionary<string, object>
{
    ["X"] = 100.5,
    ["Y"] = 200.3,
    ["Z"] = 50.0
});
```

---

### 3.2. Таблица переменных (VariableTableSet)

**Иерархия:**
```
VariableTableSet (имя: "GlobalVariables")
├── VariableTable (тип: int)
│   ├── Variable("Counter", int, 0)
│   └── Variable("Sensor1", int, 100)
├── VariableTable (тип: double)
│   └── Variable("Pi", double, 3.14159)
└── VariableTable (тип: string)
    └── Variable("Status", string, "Idle")
```

**Методы:**
```csharp
public class VariableTableSet
{
    string Name { get; set; }
    bool IsGlobal { get; set; }
    
    VariableTable GetOrCreateTable(DataType type);
    Variable? FindVariable(string name);
    void AddVariable(Variable variable, DataTypeRegistry registry);
}
```

---

### 3.3. Глобальные и локальные таблицы

| Тип | Область | Время жизни |
|-----|---------|-------------|
| **Глобальные** | Весь проект | Пока проект загружен |
| **Локальные** | Программа | Пока программа выполняется |

**Доступ:**
```csharp
// Глобальные
project.GlobalTables.FindVariable("Counter");

// Локальные (в программе)
program.LocalTables.FindVariable("LocalVar");
```

---

## 4. Привязки (Binding)

### 4.1. Направления привязки

```csharp
public enum BindingDirection
{
    OneWayToSource,      // Target → Source
    OneWayToTarget,      // Source → Target
    Bidirectional        // Source ↔ Target
}
```

### 4.2. Создание привязки

```csharp
var binding = BindingManager.Bind(
    sourceVariable: "Sensor1",
    targetVariable: "GlobalCounter",
    direction: BindingDirection.OneWayToTarget
);
```

### 4.3. BindingRouter

**Маршрутизация изменений:**
```csharp
public class BindingRouter
{
    void RouteChange(string variableName, object? newValue);
    void RegisterBinding(Binding binding);
    void UnregisterBinding(Binding binding);
}
```

**Пример:**
```csharp
// При изменении Sensor1 автоматически обновится GlobalCounter
BindingManager.Bind("Sensor1", "GlobalCounter", BindingDirection.OneWayToTarget);

// Изменение
sensor1Var.Value = 50;
// → BindingRouter.RouteChange("Sensor1", 50)
// → GlobalCounter автоматически станет 50
```

---

## 5. Программы и инструкции

### 5.1. Программа (AstroProgram)

```csharp
public class AstroProgram
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string ReturnTypeId { get; set; }
    public bool IsBackground { get; set; }
    public int MaxCycles { get; set; }
    
    public List<Argument> Arguments { get; set; }
    public List<Instruction> Lines { get; set; }
    public VariableTableSet LocalTables { get; set; }
    public Dictionary<string, int> Labels { get; set; }
}
```

---

### 5.2. Аргументы программы

```csharp
public enum ArgumentDirection
{
    In,    // Только вход
    Out,   // Только выход
    InOut  // Вход и выход
}

public class Argument
{
    public string Name { get; set; }
    public string TypeId { get; set; }
    public ArgumentDirection Direction { get; set; }
    public object? DefaultValue { get; set; }
}
```

**Пример:**
```csharp
program.Arguments.Add(new Argument
{
    Name = "StartValue",
    TypeId = "int",
    Direction = ArgumentDirection.In,
    DefaultValue = 0
});
```

---

### 5.3. Инструкция (Instruction)

```csharp
public class Instruction
{
    public int LineNumber { get; set; }
    public string FormId { get; set; }  // "core.assign", "core.if", ...
    public Dictionary<string, FieldValue> Fields { get; set; }
    public string? Comment { get; set; }
}
```

**Типы полей:**
```csharp
public abstract class FieldValue { }
public class ConstantFieldValue : FieldValue { public object? Value; }
public class VariableFieldValue : FieldValue { public string TableSetName; public string VariableName; }
public class ExpressionFieldValue : FieldValue { public string Expression; }
```

**Пример создания:**
```csharp
var instruction = new Instruction(1, "core.assign")
{
    Fields = new Dictionary<string, FieldValue>
    {
        ["variable"] = new VariableFieldValue("LocalVariables", "Counter", "int"),
        ["expression"] = new ExpressionFieldValue("Counter + 1")
    },
    Comment = "Инкремент счётчика"
};
```

---

### 5.4. Метки (Labels)

```csharp
// Определение метки
program.Lines.Add(new Instruction(line++, "core.lbl")
{
    Fields = new() { ["labelName"] = new ConstantFieldValue("START") }
});

// Переход к метке
program.Lines.Add(new Instruction(line++, "core.jumplbl")
{
    Fields = new() { ["labelName"] = new ConstantFieldValue("START") }
});

// Регистрация позиции
program.Labels["START"] = 3;  // Строка 3
```

---

## 6. Формы инструкций

### 6.1. Форма (FormDefinition)

```csharp
public class FormDefinition
{
    public string Id { get; set; }           // "core.assign"
    public string Name { get; set; }         // "Assign"
    public string Category { get; set; }     // "Assignment"
    public string? Description { get; set; }
    public FormAccessLevel AccessLevel { get; set; }
    public bool IsControlFlow { get; set; }
    public ControlFlowStructure? ControlFlow { get; set; }
    public List<FormFieldDefinition> Fields { get; set; }
}
```

---

### 6.2. Поля формы (FormFieldDefinition)

```csharp
public class FormFieldDefinition
{
    public string Name { get; set; }              // "variable", "expression"
    public string DisplayName { get; set; }       // "Переменная", "Выражение"
    public FieldValueType ValueType { get; set; } // Constant, Variable, Expression, Enum
    public List<string> AllowedTypeIds { get; set; }
    public bool Required { get; set; }
    public FieldValue? DefaultValue { get; set; }
    public List<string>? Options { get; set; }    // Для Enum
}
```

**Типы значений:**
```csharp
public enum FieldValueType
{
    Constant,    // Константа
    Variable,    // Переменная
    Expression,  // Выражение
    Enum         // Перечисление
}
```

---

### 6.3. Реестр форм (FormRegistry)

```csharp
public class FormRegistry
{
    void RegisterForm(FormDefinition form);
    FormDefinition? GetFormById(string id);
    IEnumerable<FormDefinition> AllForms { get; }
}
```

**Пример регистрации:**
```csharp
project.FormRegistry.RegisterForm(BuiltinForms.CreateAssignmentForm());
project.FormRegistry.RegisterForm(BuiltinForms.CreateIfForm());
```

---

## 7. Выражения и функции

### 7.1. Парсер выражений (ExpressionParser)

**Поддерживаемые операторы:**

| Категория | Операторы |
|-----------|-----------|
| **Арифметика** | `+`, `-`, `*`, `/`, `%`, `^` |
| **Сравнение** | `==`, `!=`, `<`, `>`, `<=`, `>=` |
| **Логика** | `&&`, `||`, `!` |
| **Битовые** | `&`, `|`, `^`, `~`, `<<`, `>>` |

**Пример:**
```csharp
var parser = new ExpressionParser();
var node = parser.Parse("(Counter % 2) == 0 && Value > 10");
```

---

### 7.2. Вычислитель (ExpressionEvaluator)

```csharp
public class ExpressionEvaluator
{
    object? Evaluate(AstNode node, ExpressionContext context);
}
```

**Контекст:**
```csharp
public class ExpressionContext
{
    public VariableTableSet GlobalTables { get; set; }
    public VariableTableSet LocalTables { get; set; }
    public DataTypeRegistry TypeRegistry { get; set; }
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; }
}
```

---

### 7.3. Встроенные функции

#### Математические
- `SIN`, `COS`, `TAN`, `ASIN`, `ACOS`, `ATAN`
- `SQRT`, `EXP`, `LOG`, `LOG10`
- `ABS`, `ROUND`, `FLOOR`, `CEIL`

#### Строковые
- `LEN`, `CONCAT`, `SUBSTR`
- `UPPER`, `LOWER`, `TRIM`

#### Массивы
- `SIZE`, `ADD`, `REMOVE`, `FIND`
- `SLICE`, `RANGE`, `EMPTY`
- `CONTAINS`, `INDEXOF`, `REVERSE`
- `SUM`, `MIN`, `MAX`, `AVERAGE`

**Пример использования:**
```csharp
// В выражении
AI("core.assign", new() {
    ["variable"] = new VariableFieldValue("LocalVariables", "Size", "int"),
    ["expression"] = new ExpressionFieldValue("SIZE(MyArray)")
});
```

---

### 7.4. Регистрация функций

```csharp
// Встроенные функции
project.Functions["SIN"] = args => Math.Sin(Convert.ToDouble(args[0]));

// Пользовательская функция
project.Functions["MY_FUNC"] = args => {
    var result = Convert.ToDouble(args[0]) * 2;
    return result;
};
```

---

## 8. Интерпретатор

### 8.1. AstroInterpreter

**Основные методы:**
```csharp
public class AstroInterpreter
{
    void LoadProgram(AstroProgram program, VariableTableSet? localTables);
    void Run();           // Выполнение до конца
    void Step();          // Один шаг (для отладки)
    void Stop();          // Остановка
    void Pause();         // Пауза
    void Resume();        // Продолжение
    void Reset();         // Сброс состояния
}
```

---

### 8.2. Состояние интерпретатора (InterpreterState)

```csharp
public class InterpreterState
{
    public int CurrentLineIndex { get; set; }
    public Stack<CallFrame> CallStack { get; set; }
    public Stack<LoopContext> LoopStack { get; set; }
    public Stack<SwitchContext> SwitchStack { get; set; }
    public Stack<ExceptionContext> ExceptionStack { get; set; }  // TRY/CATCH
    public bool StopRequested { get; set; }
    public bool PauseRequested { get; set; }
    public object? ReturnValue { get; set; }
    public VariableTableSet CurrentLocalTables { get; set; }
    public AstroProgram Program { get; set; }
}
```

---

### 8.3. Контекст интерпретатора (InterpreterContext)

```csharp
public class InterpreterContext
{
    public DataTypeRegistry TypeRegistry { get; set; }
    public FormRegistry FormRegistry { get; set; }
    public VariableTableSet GlobalTables { get; set; }
    public Dictionary<string, Func<object?[], object?>> Functions { get; set; }
    public Dictionary<string, AstroProgram> ProgramRegistry { get; set; }
    
    // События
    event Action<InterpreterState, Instruction> OnBeforeInstruction;
    event Action<InterpreterState, Instruction> OnAfterInstruction;
    event Action<InterpreterState, Exception> OnError;
}
```

---

### 8.4. Обработка инструкций

**Атрибуты для регистрации:**
```csharp
[InstructionHandler("core.assign")]
private void ExecuteAssign(Instruction instruction)
{
    var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
    var exprField = GetFieldValue<ExpressionFieldValue>(instruction, "expression");
    
    var variable = FindVariable(varField.TableSetName, varField.VariableName);
    var value = EvaluateExpression(exprField.Expression);
    
    variable.Value = value;
}
```

---

## 9. Обработка исключений

### 9.1. Инструкции

| Инструкция | Описание |
|------------|----------|
| `TRY` | Начало блока обработки |
| `CATCH` | Перехват исключения |
| `FINALLY` | Блок выполняется всегда |
| `ENDTRY` | Конец блока |
| `THROW` | Выброс исключения |
| `RETHROW` | Повторный выброс |

---

### 9.2. Пример использования

```astro
TRY
  Result = Value1 / Value2
  IF Result < 0 THEN
    THROW ErrorCode=1, Message="Negative result"
  ENDIF
CATCH ErrMsg
  LogError("Calculation failed: " + ErrMsg)
  Result = 0
FINALLY
  DisplayResult(Result)
ENDTRY
```

---

### 9.3. Контекст исключения (ExceptionContext)

```csharp
public class ExceptionContext
{
    public int TryStartLineIndex { get; set; }
    public int? CatchLineIndex { get; set; }
    public int? FinallyLineIndex { get; set; }
    public int EndLineIndex { get; set; }
    public string? ExceptionVariableName { get; set; }
    public int ErrorCodeFilter { get; set; }
    public bool ExceptionCaught { get; set; }
    public bool FinallyExecuted { get; set; }
    public string? ExceptionMessage { get; set; }
    public int ExceptionCode { get; set; }
}
```

---

## 10. Аварии (Alarms)

### 10.1. Тяжесть аварий

```csharp
public enum AlarmSeverity
{
    Info,     // Информация
    Warning,  // Предупреждение
    Error,    // Ошибка
    Fatal     // Критическая (вызывает исключение)
}
```

---

### 10.2. Определение аварии (AlarmDefinition)

```csharp
public class AlarmDefinition
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string MessageTemplate { get; set; }  // "Error on line {0}"
    public AlarmSeverity Severity { get; set; }
    public bool IsSystemAlarm { get; set; }
    
    string FormatMessage(params object[] args);
}
```

**Пример:**
```csharp
alarms.CreateUserAlarm(
    name: "SAFETY_DOOR",
    messageTemplate: "Safety door is open on line {0}",
    severity: AlarmSeverity.Fatal
);
```

---

### 10.3. Экземпляр аварии (AlarmInstance)

```csharp
public class AlarmInstance
{
    public AlarmDefinition Definition { get; set; }
    public DateTime RaisedTime { get; set; }
    public DateTime? AcknowledgedTime { get; set; }
    public DateTime? ClearedTime { get; set; }
    public AlarmState State { get; set; }  // Active, Acknowledged, Cleared
    public object[]? Parameters { get; set; }
}
```

---

### 10.4. AlarmManager

**Методы:**
```csharp
public class AlarmManager : IAlarmService
{
    // Регистрация
    void RegisterAlarm(AlarmDefinition definition);
    void CreateUserAlarm(string name, string messageTemplate, AlarmSeverity severity);
    
    // Управление
    void Raise(int code, params object[] parameters);
    void Acknowledge(int code);
    void Clear(int code);
    void ClearAll();
    
    // Получение
    AlarmDefinition? GetDefinition(int code);
    AlarmInstance? GetActiveAlarm(int code);
    
    // События
    event Action<AlarmInstance> OnAlarmRaised;
    event Action<AlarmInstance> OnAlarmAcknowledged;
    event Action<AlarmInstance> OnAlarmCleared;
}
```

---

## 11. Прерывания (Interrupts)

### 11.1. Типы триггеров

```csharp
public enum InterruptTrigger
{
    OnChange,        // Изменение переменной
    OnRisingEdge,    // Переход 0→1
    OnFallingEdge,   // Переход 1→0
    OnValue,         // Достигнуто значение
    OnTimer,         // Таймер
    OnAlarm          // Авария
}
```

---

### 11.2. Режимы выполнения

```csharp
public enum InterruptExecutionMode
{
    Deferred,    // Отложенное (в очереди)
    Background,  // Фоновое
    Inline       // Немедленное
}
```

---

### 11.3. InterruptDefinition

```csharp
public class InterruptDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public InterruptTrigger TriggerType { get; set; }
    public InterruptExecutionMode ExecutionMode { get; set; }
    public bool IsEnabled { get; set; }
    
    // Для OnChange/OnValue
    public string? VariableName { get; set; }
    public string? Expression { get; set; }
    
    // Для OnAlarm
    public int? AlarmCode { get; set; }
    
    // Для OnTimer
    public int? TimerIntervalMs { get; set; }
    
    // Обработчик
    public string? HandlerProgramName { get; set; }
}
```

---

### 11.4. InterruptManager

```csharp
public class InterruptManager : IInterruptService
{
    void Register(InterruptDefinition definition);
    void Unregister(string id);
    void Fire(InterruptDefinition definition);
    
    bool HasDeferred { get; }
    InterruptDefinition? DequeueDeferred();
    
    // Мониторинг
    void StartMonitoring();
    void StopMonitoring();
}
```

---

## 12. Таймеры

### 12.1. Режимы таймера

```csharp
public enum TimerMode
{
    Periodic,   // Периодический
    Oneshot     // Однократный
}
```

---

### 12.2. TimerDefinition

```csharp
public class TimerDefinition
{
    public string Name { get; set; }
    public int IntervalMs { get; set; }
    public TimerMode Mode { get; set; }
    public bool IsEnabled { get; set; }
}
```

---

### 12.3. TimerInstance

```csharp
public class TimerInstance
{
    public TimerDefinition Definition { get; set; }
    public DateTime LastElapsed { get; set; }
    public int ElapsedCount { get; set; }
    public bool IsRunning { get; set; }
}
```

---

### 12.4. TimerManager

```csharp
public class TimerManager : ITimerService
{
    void Register(TimerDefinition definition);
    void Start(string name);
    void Stop(string name);
    void Disable(string name);
    void Reset(string name);
    
    event Action<TimerInstance> OnTimerElapsed;
    
    void Start();   // Запуск мониторинга
    void Stop();    // Остановка
}
```

---

## 13. Планировщик задач

### 13.1. Типы задач

```csharp
public enum TaskType
{
    Foreground,  // Передний план (основная)
    Background   // Задний план (фоновая)
}
```

---

### 13.2. Приоритеты

```csharp
public enum TaskPriority
{
    Low,
    Normal,
    High,
    Critical
}
```

---

### 13.3. TaskConfig

```csharp
public class TaskConfig
{
    public int TaskId { get; set; }
    public string Name { get; set; }
    public AstroProgram Program { get; set; }
    public TaskType Type { get; set; }
    public TaskPriority Priority { get; set; }
    public int CycleIntervalMs { get; set; }
    public int MaxCycles { get; set; }
}
```

---

### 13.4. TaskScheduler

```csharp
public class TaskScheduler
{
    void StartTask(TaskConfig config);
    void StopTask(int taskId);
    void PauseTask(int taskId);
    void ResumeTask(int taskId);
    
    void StartScheduler();
    void StopScheduler();
    
    event Action<TaskRunner> OnTaskStarted;
    event Action<TaskRunner> OnTaskStopped;
}
```

---

## 14. Система плагинов

### 14.1. Интерфейс плагина

```csharp
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    void OnLoad(PluginContext context);
    void OnUnload();
}
```

---

### 14.2. PluginContext

```csharp
public class PluginContext
{
    Action<string, Action<Instruction>> RegisterInstruction;
    Action<string, IBuiltinFunction> RegisterFunction;
    Action<FormDefinition> RegisterForm;
    Action<DataType> RegisterType;
    Action<string, string> Log;
}
```

---

### 14.3. PluginManager

```csharp
public class PluginManager
{
    void LoadAllPlugins();      // Загрузка из папки Plugins/
    void LoadPlugin(string dllPath);
    void UnloadPlugin(string name);
    void UnloadAll();
    
    // Для скриптов
    ScriptPluginLoader? ScriptLoader { get; }
    CSharpScriptEngine? ScriptEngine { get; }
}
```

---

### 14.4. Runtime компиляция

```csharp
// Загрузка .cs файла с компиляцией
scriptLoader.CompileAndLoadScript("Plugins/Scripts/MyPlugin.cs");

// Горячая перезагрузка
scriptLoader.ReloadScript("Plugins/Scripts/MyPlugin.cs");
```

---

### 14.5. C# скрипты

```csharp
// Выполнение скрипта
var result = scriptEngine.ExecuteScript(@"
    var x = 10;
    var y = 20;
    return x + y;
");
```

---

## 15. Сериализация и хранение

### 15.1. Структура проекта

```
MyProject/
├── AstroData/
│   ├── Registry/
│   │   ├── types.json       # Типы данных
│   │   ├── forms.json       # Формы
│   │   ├── globals.json     # Глобальные переменные
│   │   └── alarms.json      # Аварии
│   ├── Programs/
│   │   ├── Main.ast         # Программа Main
│   │   └── Sub.ast          # Программа Sub
│   └── Plugins/
│       ├── MyPlugin.dll     # Плагин
│       └── Scripts/
│           └── MyScript.cs  # Скрипт
└── project.json
```

---

### 15.2. AstroSerializer

```csharp
public static class AstroSerializer
{
    // Типы
    void SaveDataTypeRegistry(DataTypeRegistry registry, string folder);
    DataTypeRegistry LoadDataTypeRegistry(string folder);
    
    // Формы
    void SaveFormRegistry(FormRegistry registry, string folder);
    FormRegistry LoadFormRegistry(string folder);
    
    // Переменные
    void SaveGlobalTables(VariableTableSet tables, string folder);
    VariableTableSet LoadGlobalTables(string folder, DataTypeRegistry types);
    
    // Программы
    void SaveProgram(AstroProgram program, string folder);
    AstroProgram LoadProgram(string folder, string name, DataTypeRegistry types);
}
```

---

### 15.3. Формат файлов

**types.json:**
```json
{
  "Types": [
    {
      "$type": "EnumDataType",
      "Id": "color",
      "Name": "COLOR",
      "Category": "User",
      "Values": { "RED": 0, "GREEN": 1, "BLUE": 2 }
    }
  ]
}
```

**programs/Main.ast:**
```json
{
  "Name": "Main",
  "Version": "1.0",
  "Arguments": [...],
  "LocalVariables": [...],
  "Lines": [
    {
      "LineNumber": 1,
      "FormId": "core.assign",
      "Fields": { ... }
    }
  ]
}
```

---

## 📚 Приложения

### A. Список всех инструкций

| Категория | Инструкции |
|-----------|------------|
| **Присваивание** | `core.assign` |
| **Циклы** | `core.while`, `core.endwhile`, `core.for`, `core.endfor`, `core.foreach`, `core.endforeach` |
| **Условия** | `core.if`, `core.else`, `core.endif`, `core.switch`, `core.case`, `core.default`, `core.endswitch` |
| **Переходы** | `core.lbl`, `core.jumplbl`, `core.jumpif` |
| **Вызовы** | `core.call`, `core.return` |
| **Управление** | `core.break`, `core.continue` |
| **Аварии** | `core.alarm.raise`, `core.alarm.clear`, `core.alarm.ack`, `core.alarm.clearall` |
| **Прерывания** | `core.interrupt.declare`, `core.interrupt.on`, `core.interrupt.off` |
| **Таймеры** | `core.timer.declare`, `core.timer.on`, `core.timer.off`, `core.timer.reset` |
| **Ожидание** | `core.wait` |
| **Исключения** | `core.try`, `core.catch`, `core.finally`, `core.endtry`, `core.throw`, `core.rethrow` |

---

### B. Быстрый старт

```csharp
// 1. Инициализация
var project = new ProjectManager();
project.InitializeNew("MyProject");

// 2. Создание типа
var colorType = new EnumDataType { ... };
project.TypeRegistry.RegisterType(colorType);

// 3. Создание переменной
project.GlobalTables.GetOrCreateTable(intType).AddVariable(
    new Variable("Counter", intType, 0)
);

// 4. Создание программы
var program = new AstroProgram { Name = "Main" };
program.Lines.Add(new Instruction(1, "core.assign") { ... });
project.AddProgram(program);

// 5. Запуск
var interpreter = project.CreateInterpreter();
interpreter.LoadProgram(program);
interpreter.Run();
```

---

### C. Полезные ссылки

- [Модульная структура](./ModularStructure.md)
- [Система плагинов](./Plugins.md)
- [Runtime компиляция](./RuntimeCompilation.md)
- [Обработка исключений](./ExceptionHandling.md)

---

**Версия документа:** 4.0  
**Последнее обновление:** 2024  
**Поддерживаемые версии AstroEditor:** 4.x
