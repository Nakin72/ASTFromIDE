# Thread Safety, Логирование и Кэширование в ASTRO Editor v4

## Обзор изменений

В рамках аудита кодовой базы и исправления проблем приоритетов 1-2 были реализованы следующие улучшения:

### 1. ✅ Логирование (Logging)

**Файл:** `Core/Common/Logging/LoggingService.cs`

**Возможности:**
- Централизованная служба логирования через `Microsoft.Extensions.Logging`
- Уровни логирования: Trace, Debug, Information, Warning, Error, Critical
- Вывод в консоль с цветовой схемой
- Статический класс `Log` для быстрого доступа

**Использование:**
```csharp
using AstroEditor.Core.Common.Logging;
using Microsoft.Extensions.Logging;

// В классе
private readonly ILogger _logger = Log.For<MyClass>();

// Логирование
_logger.LogDebug("Debug message");
_logger.LogInformation("Info: {VariableName} = {Value}", name, value);
_logger.LogWarning("Warning message");
_logger.LogError(ex, "Error occurred");
```

**Инициализация в Program.cs:**
```csharp
LoggingService.Initialize(LogLevel.Information);
// ... работа приложения ...
LoggingService.Shutdown();
```

---

### 2. ✅ Потокобезопасный менеджер привязок

**Файл:** `Core/Binding/ThreadSafeBindingManager.cs`

**Проблема:** Глобальные таблицы переменных должны быть потокобезопасными, так как любые программы могут читать/записывать значения и создавать новые переменные.

**Решение:**
- `ConcurrentDictionary<string, Binding>` для хранения привязок
- `ConcurrentDictionary<string, Variable?>` для кэширования переменных
- `ReaderWriterLockSlim` для безопасной записи значений
- Интерфейс `IBindingService` для внедрения зависимостей

**Ключевые методы:**
```csharp
public class ThreadSafeBindingManager : IBindingService
{
    Binding Bind(string source, string target, BindingDirection direction);
    void UpdateValue(string variableName, object? newValue); // Thread-safe
    IReadOnlyCollection<Binding> GetAllBindings();
    void InvalidateAllCache();
}
```

**Использование для IO-контактов:**
```csharp
// Любая программа может безопасно читать/писать
bindings.UpdateValue("DO[1]", true);  // Thread-safe запись
var value = globalTables.FindVariable("DO[1]")?.Value;  // Чтение
```

---

### 3. ✅ Кэширование AST выражений

**Файлы:**
- `Core/Expressions/ExpressionCache.cs`
- `Core/Expressions/IExpressionCache.cs`

**Проблема:** AST парсилось при каждом выполнении инструкции, что неэффективно.

**Решение:**
- Кэширование разобранных выражений по строковому ключу
- Статистика hit/miss для мониторинга эффективности
- Метод `PreCache()` для предварительной загрузки

**Статистика:**
```csharp
var stats = cache.GetStatistics();
Console.WriteLine($"Hit Rate: {stats.HitRate:F2}%");
Console.WriteLine($"Cache Size: {stats.CacheSize}");
```

**Использование в интерпретаторе:**
```csharp
// Вместо _parser.Parse(expr)
var exprNode = _expressionCache != null 
    ? _expressionCache.GetOrParse(exprField.Expression)
    : _parser.Parse(exprField.Expression);
```

---

### 4. ✅ Расширенный интерпретатор

**Файл:** `Core/Interpreter/AstroInterpreterEx.cs`

**Новые возможности:**
- Внедрение зависимостей (ILogger, IExpressionCache)
- Логирование всех этапов выполнения
- Статистика выполнения (`InterpreterStatistics`)
- Прекомпиляция выражений при загрузке программы

**Статистика выполнения:**
```csharp
var stats = interpreter.GetStatistics();
Console.WriteLine($"Instructions: {stats.InstructionsExecuted}");
Console.WriteLine($"Duration: {stats.Duration}");
Console.WriteLine($"Speed: {stats.InstructionsPerSecond:F0} instr/s");
```

**Логирование:**
- Загрузка программы
- Выполнение инструкций (Trace уровень)
- Ошибки выполнения
- Завершение программы

---

### 5. ✅ Интерфейс контекста выполнения

**Файл:** `Core/Data/IRuntimeContext.cs`

**Назначение:** Унифицированный интерфейс для внедрения зависимостей в плагины и расширения.

**Интерфейс включает:**
```csharp
public interface IRuntimeContext
{
    VariableTableSet GlobalTables { get; }
    ITypeRegistry TypeRegistry { get; }
    ITimerService Timers { get; }
    ITaskScheduler Scheduler { get; }
    IBindingService Bindings { get; }
    IExpressionCache ExpressionCache { get; }
    FormRegistry Forms { get; }
    AstroProgram? CurrentProgram { get; }
}
```

**Реализация:** `InterpreterContextEx`

---

### 6. ✅ Изоляция плагинов

**Файл:** `Core/Plugins/PluginSandbox.cs`

**Проблема:** Плагины загружались в основной контекст, что не позволяло их выгрузить.

**Решение:**
- `AssemblyLoadContext(isCollectible: true)` для изолированной загрузки
- Возможность выгрузки плагинов через `Unload()`
- Безопасное выполнение кода плагинов

**Использование:**
```csharp
var sandbox = new PluginSandbox();
var plugin = sandbox.LoadPlugin("path/to/plugin.dll", context);
plugin.Initialize();

// При необходимости выгрузить
sandbox.UnloadPlugin(plugin);
```

---

## Интеграция в существующий код

### Обновление ProjectManager

```csharp
// Создать сервисы
var bindingService = new ThreadSafeBindingManager(globalTables, logger);
var expressionCache = new ExpressionCache(maxSize: 1000);

// Передать в интерпретатор
var interpreter = new AstroInterpreterEx(
    context,
    pluginManager: pluginManager,
    expressionCache: expressionCache,
    logger: Log.For<AstroInterpreterEx>()
);
```

### Обновление глобальных таблиц для IO

```csharp
// В ProjectManager или RuntimeContext
private readonly ThreadSafeBindingManager _bindings;

// При обновлении IO-контакта
public void UpdateDigitalOutput(int channel, bool value)
{
    var varName = $"DO[{channel}]";
    _bindings.UpdateValue(varName, value);  // Thread-safe
}

// Чтение IO-контакта
public bool ReadDigitalInput(int channel)
{
    var varName = $"DI[{channel}]";
    return (bool?)(_globalTables.FindVariable(varName)?.Value) ?? false;
}
```

---

## Преимущества

| Компонент | Было | Стало |
|-----------|------|-------|
| **Логирование** | Console.WriteLine | Структурированное с уровнями |
| **Привязки** | Статический класс | Thread-safe сервис |
| **AST** | Парсинг каждый раз | Кэширование с hit-rate |
| **Плагины** | Без изоляции | AssemblyLoadContext |
| **Статистика** | Отсутствовала | InterpreterStatistics |

---

## Следующие шаги (Приоритет 3-4)

- [ ] Интеграция `ThreadSafeBindingManager` в `ProjectManager`
- [ ] Обновление всех мест использования `BindingManager`
- [ ] Unit-тесты для `ThreadSafeBindingManager`
- [ ] Unit-тесты для `ExpressionCache`
- [ ] Интеграция `LoggingService` во все компоненты
- [ ] Опциональный IO-сервис с thread-safe доступом

---

## Примечания

- Все предупреждения nullable reference types можно игнорировать или исправить постепенно
- Для production рекомендуется включить логирование в файл
- Кэш выражений можно настроить через `maxCacheSize` (по умолчанию 1000)
