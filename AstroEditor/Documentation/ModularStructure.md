# 📁 Модульная структура AstroEditor v4

## 🎯 Принципы разбиения

1. **Single Responsibility** — каждый файл отвечает за одну функцию
2. **Partial классы** — логическое разделение больших классов
3. **Категоризация** — группировка по доменной области
4. **Расширяемость** — лёгкое добавление нового функционала

---

## 📊 Результаты разбиения

### До

| Файл | Строк | Проблема |
|------|-------|----------|
| `AstroInterpreter.cs` | 1200+ | Монолит, сложно поддерживать |
| `BuiltinForms.cs` | 908 | Все формы в одном файле |
| `BuiltinFunctions.cs` | 237 | Смешаны разные категории |
| `ProjectManager.cs` | 441 | Много ответственности |
| `AlarmManager.cs` | 276 | Можно разбить |

### После

| Категория | Файлы | Строк в каждом |
|-----------|-------|----------------|
| **AstroInterpreter** | 10 файлов | 50-200 |
| **BuiltinForms** | 5 файлов | 100-250 |
| **BuiltinFunctions** | 4 файла | 50-150 |
| **Плагины** | 8 файлов | 100-200 |

---

## 🏗️ Структура файлов

### AstroInterpreter (Ядро интерпретатора)

```
Core/Interpreter/
├── AstroInterpreter.cs           # Ядро: Run(), Step(), LoadProgram()
├── AstroInterpreter.Core.cs      # Assign
├── AstroInterpreter.Loops.cs     # WHILE, FOR, FOREACH
├── AstroInterpreter.Conditions.cs# IF, SWITCH, BREAK, CONTINUE
├── AstroInterpreter.Calls.cs     # CALL, RETURN, JUMPS
├── AstroInterpreter.Alarms.cs    # Аварии
├── AstroInterpreter.Interrupts.cs# Прерывания
├── AstroInterpreter.Timers.cs    # Таймеры
├── AstroInterpreter.Wait.cs      # WAIT
├── AstroInterpreter.Helpers.cs   # Вспомогательные методы
├── AstroInterpreter.Registration.cs # Регистрация внешних обработчиков
├── InstructionHandlerAttribute.cs   # Атрибут [InstructionHandler]
└── IInstructionHandler.cs          # Интерфейс для плагинов
```

**Принцип:** Разбиение по типу инструкций

---

### BuiltinForms (Формы инструкций)

```
Core/Forms/
├── BuiltinForms.Core.cs          # Assign, Call, Label, Jump, Return, Break, Continue
├── BuiltinForms.Loops.cs         # WHILE, FOR, FOREACH + END*
├── BuiltinForms.Conditions.cs    # IF, ELSE, SWITCH, CASE, DEFAULT
├── BuiltinForms.Alarms.cs        # RaiseAlarm, ClearAlarm, AckAlarm, ClearAll
└── BuiltinForms.Execution.cs     # Interrupts, Timers, WAIT
```

**Принцип:** Разбиение по категориям функциональности

---

### BuiltinFunctions (Встроенные функции)

```
Core/Expressions/
├── BuiltinFunctions.cs           # Главный файл: агрегация всех функций
├── BuiltinFunctions.Math.cs      # SIN, COS, SQRT, ABS, ROUND, ...
├── BuiltinFunctions.Strings.cs   # LEN, CONCAT, SUBSTR, UPPER, LOWER, TRIM
└── BuiltinFunctions.Arrays.cs    # SIZE, ADD, REMOVE, FIND, SLICE, SUM, MIN, MAX, ...
```

**Принцип:** Разбиение по типам данных

---

### Plugins (Система плагинов)

```
Core/Plugins/
├── IPlugin.cs                    # Интерфейс плагина
├── PluginAttribute.cs            # Атрибут [Plugin]
├── PluginContext.cs              # Контекст для регистрации API
├── PluginManager.cs              # Менеджер плагинов
├── ScriptPluginLoader.cs         # Загрузчик .cs скриптов
├── CSharpScriptEngine.cs         # Движок для .csx скриптов
├── IInstructionHandler.cs        # Интерфейс обработчика инструкций
└── InstructionHandlerAttribute.cs# Атрибут для инструкций
```

**Принцип:** Разбиение по ответственности

---

## 🔧 Как добавлять новые компоненты

### 1. Новая инструкция

**Шаг 1:** Создать обработчик в `AstroInterpreter.MyFeature.cs`:

```csharp
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreter
{
    [InstructionHandler("core.my_instruction")]
    private void ExecuteMyInstruction(Instruction instruction)
    {
        // Логика инструкции
        var field = GetFieldValue<ConstantFieldValue>(instruction, "myField");
        Console.WriteLine($"My instruction: {field.Value}");
    }
}
```

**Шаг 2:** Создать форму в `BuiltinForms.MyFeature.cs`:

```csharp
using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateMyInstructionForm()
    {
        return new FormDefinition
        {
            Id = "core.my_instruction",
            Name = "MyInstruction",
            Category = "My Category",
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "myField",
                    DisplayName = "Моё поле",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }
}
```

**Шаг 3:** Зарегистрировать форму в `ProjectManager.cs`:

```csharp
FormRegistry.RegisterForm(BuiltinForms.CreateMyInstructionForm());
```

---

### 2. Новая встроенная функция

**Шаг 1:** Создать файл `BuiltinFunctions.MyCategory.cs`:

```csharp
namespace AstroEditor.Core.Expressions;

public static partial class BuiltinFunctions
{
    public static Dictionary<string, Func<object?[], object?>> GetMyCategoryFunctions()
    {
        return new Dictionary<string, Func<object?[], object?>>
        {
            { "MY_FUNC", args => /* логика */ },
        };
    }
}
```

**Шаг 2:** Добавить в главный файл `BuiltinFunctions.cs`:

```csharp
public static Dictionary<string, Func<object?[], object?>> GetFunctions()
{
    var functions = new Dictionary<string, Func<object?[], object?>>();

    // ... существующие ...

    // Моя категория
    foreach (var kv in GetMyCategoryFunctions())
        functions[kv.Key] = kv.Value;

    return functions;
}
```

---

### 3. Плагин (расширение)

**Вариант A: Runtime Compilation (.cs)**

Создать файл `Plugins/Scripts/MyPlugin.cs`:

```csharp
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;

namespace AstroEditor.Plugins.Scripts;

[Plugin("MyPlugin", "1.0", "Описание")]
public class MyPlugin : IPlugin
{
    public void OnLoad(PluginContext context)
    {
        context.RegisterInstruction("myplugin.test", ExecuteTest);
    }

    public void OnUnload() { }

    private void ExecuteTest(Instruction instruction)
    {
        Console.WriteLine("Test from plugin!");
    }
}
```

**Вариант B: Pre-compiled DLL**

1. Создать проект библиотеки классов
2. Добавить ссылку на AstroEditor
3. Написать плагин
4. Скомпилировать в .dll
5. Скопировать в `Plugins/`

---

### 4. Новая форма

Создать файл `BuiltinForms.MyCategory.cs` (или добавить в существующий):

```csharp
using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static partial class BuiltinForms
{
    public static FormDefinition CreateMyNewForm()
    {
        return new FormDefinition
        {
            Id = "core.my_new_form",
            Name = "MyNewForm",
            Category = "My Category",
            Description = "Описание формы",
            AccessLevel = FormAccessLevel.User,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                // Поля формы
            }
        };
    }
}
```

---

## 📈 Преимущества новой структуры

| Преимущество | Описание |
|--------------|----------|
| **Читаемость** | Легко найти нужный код |
| **Поддержка** | Меньше конфликтов при слиянии |
| **Тестирование** | Можно тестировать отдельные модули |
| **Расширяемость** | Добавление без изменения существующего кода |
| **Сборка** | Faster incremental builds |
| **Документация** | Файлы самодокументированы |

---

## 🎓 Лучшие практики

1. **Именование файлов** — используйте суффиксы `.Loops.cs`, `.Conditions.cs`
2. **Partial классы** — для разделения больших классов
3. **Один файл = одна ответственность** — не смешивайте разные функции
4. **Категоризация** — группируйте по доменной области
5. **Документирование** — добавляйте XML-комментарии

---

## 🔮 Планы дальнейшего разбиения

- [ ] `ProjectManager.cs` → `ProjectManager.Projects.cs`, `ProjectManager.Types.cs`, ...
- [ ] `AlarmManager.cs` → `AlarmManager.Definitions.cs`, `AlarmManager.Instances.cs`
- [ ] `ExpressionEvaluator.cs` → `ExpressionEvaluator.Parser.cs`, `ExpressionEvaluator.Executor.cs`
- [ ] `TaskScheduler.cs` → `TaskScheduler.Queue.cs`, `TaskScheduler.Execution.cs`
- [ ] `InterruptManager.cs` → `InterruptManager.Triggers.cs`, `InterruptManager.Handlers.cs`

---

## 📊 Статистика

| Метрика | Было | Стало | Улучшение |
|---------|------|-------|-----------|
| **Средний размер файла** | 350 строк | 120 строк | 2.9x |
| **Максимальный файл** | 1200 строк | 250 строк | 4.8x |
| **Файлов >200 строк** | 10 | 2 | 5x |
| **Модульность** | Низкая | Высокая | ✨ |

---

## 🚀 Быстрый старт для разработчиков

### 1. Клонировать репозиторий

```bash
git clone <repo_url>
cd AstroEditor
```

### 2. Открыть в IDE

```bash
code .  # VS Code
# или
AstroEditor.sln  # Visual Studio
```

### 3. Собрать

```bash
dotnet build
```

### 4. Запустить

```bash
dotnet run
```

### 5. Добавить новую функцию

Следуйте инструкциям выше в разделе "Как добавлять новые компоненты"

---

## 📚 Дополнительные ресурсы

- [Система плагинов](./Plugins.md)
- [Runtime компиляция](./RuntimeCompilation.md)
- [ASTRO Editor v4 Architecture](./Architecture.md)
