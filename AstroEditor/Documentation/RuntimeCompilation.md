# 🚀 Runtime компиляция и скрипты в AstroEditor

## 📖 Обзор

AstroEditor поддерживает **три способа** расширения функционала:

| Способ | Файл | Компиляция | Загрузка |
|--------|------|------------|----------|
| **1. Pre-compiled DLL** | `.dll` | Заранее (dotnet build) | Мгновенно |
| **2. Runtime Compilation** | `.cs` | При загрузке (Roslyn) | ~1-2 сек |
| **3. C# Scripting** | `.csx` | Интерпретация | ~0.5 сек |

---

## 1️⃣ Pre-compiled DLL плагины

### Структура
```
Plugins/
└── MyPlugin.dll
```

### Преимущества
- ✅ Максимальная скорость загрузки
- ✅ Можно распространять без исходного кода
- ✅ Полная типобезопасность

### Недостатки
- ❌ Требует компиляции отдельно
- ❌ Нужно перезагружать среду для обновлений

### Создание
```bash
dotnet new classlib -n MyPlugin
dotnet add reference ../AstroEditor/AstroEditor.csproj
# Пишем код...
dotnet build -c Release
cp bin/Release/net8.0/MyPlugin.dll Plugins/
```

---

## 2️⃣ Runtime Compilation (.cs файлы)

### Структура
```
Plugins/
├── Scripts/
│   ├── HelloWorldScript.cs
│   └── MyPlugin.cs
└── Cache/
    ├── HelloWorldScript.dll
    └── MyPlugin.dll
```

### Преимущества
- ✅ Пишем код в `.cs` файлах
- ✅ Автоматическая компиляция при загрузке
- ✅ Кэширование DLL для ускорения
- ✅ Горячая перезагрузка

### Недостатки
- ❌ Медленнее готовых DLL
- ❌ Исходный код виден пользователю

### Пример плагина

```csharp
// Plugins/Scripts/MyScript.cs
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;

namespace AstroEditor.Plugins.Scripts;

[Plugin("MyScript", "1.0", "Описание")]
public class MyScript : IPlugin
{
    public string Name => "MyScript";
    public string Version => "1.0";
    public string Description => "Описание";

    public void OnLoad(PluginContext context)
    {
        context.RegisterInstruction("myscript.test", ExecuteTest);
    }

    public void OnUnload() { }

    private void ExecuteTest(Instruction instruction)
    {
        Console.WriteLine("Hello from runtime compiled script!");
    }
}
```

### Загрузка
```csharp
// Автоматически при инициализации ProjectManager
var pluginManager = new PluginManager(...);
pluginManager.LoadAllPlugins(); // Загружает и .dll, и .cs

// Или вручную перекомпилировать
pluginManager.ScriptLoader?.ReloadScript("Plugins/Scripts/MyScript.cs");
```

---

## 3️⃣ C# Scripting (.csx файлы)

### Структура
```
Plugins/
└── Scripts/
    └── QuickScript.csx
```

### Преимущества
- ✅ Максимально быстро для прототипов
- ✅ Не нужно объявлять классы
- ✅ Интерактивный режим
- ✅ Доступ к глобальным переменным

### Недостатки
- ❌ Медленнее компиляции
- ❌ Нет типобезопасности на этапе компиляции
- ❌ Ошибки только во время выполнения

### Пример скрипта

```csharp
// Plugins/Scripts/QuickScript.csx
using AstroEditor.Core.Interpreter;

// Простое вычисление
var result = 10 + 20 * 3;
Console.WriteLine($"Result: {result}");

// Работа с переменными (сохраняются между запусками)
counter = counter + 1 ?? 1;
Console.WriteLine($"Counter: {counter}");

// Использование LINQ
var numbers = new[] { 1, 2, 3, 4, 5 };
return numbers.Sum();
```

### Выполнение

```csharp
// В коде AstroEditor
var engine = pluginManager.ScriptEngine;

// Выполнить скрипт
var result = engine?.ExecuteScript(File.ReadAllText("Plugins/Scripts/QuickScript.csx"));

// Выполнить с продолжением (сохранение состояния)
await engine?.ExecuteContinuationAsync("var x = 10")!;
await engine?.ExecuteContinuationAsync("x = x + 5")!;
var final = await engine?.ExecuteContinuationAsync("return x")!;
// final = 15

// Проверка синтаксиса без выполнения
if (engine?.ValidateSyntax(code, out var errors) == false)
{
    Console.WriteLine($"Errors: {string.Join("\n", errors)}");
}
```

---

## 🔥 Горячая перезагрузка скриптов

```csharp
// 1. Изменили файл Scripts/MyScript.cs в редакторе

// 2. Перекомпилировали
pluginManager.ScriptLoader?.ReloadScript("Plugins/Scripts/MyScript.cs");

// 3. Готово! Новый код активен без перезапуска AstroEditor
```

---

## 🎯 Сценарии использования

### Runtime Compilation (.cs)

| Сценарий | Описание |
|----------|----------|
| **Быстрая разработка** | Не нужно каждый раз компилировать в DLL |
| **Кастомизация** | Пользователи пишут свои плагины |
| **A/B тестирование** | Быстрое переключение версий |
| **Обучение** | Примеры кода работают из коробки |

### C# Scripting (.csx)

| Сценарий | Описание |
|----------|----------|
| **Прототипирование** | Быстрая проверка идей |
| **Макросы** | Автоматизация рутинных задач |
| **Консоль отладки** | Интерактивное выполнение команд |
| **Скрипты сборки** | Пред/пост обработка проектов |

---

## 📊 Сравнение производительности

| Операция | DLL | .cs | .csx |
|----------|-----|-----|------|
| Загрузка | ~10ms | ~1-2s | ~500ms |
| Выполнение | 100% | 100% | ~95% |
| Память | Низкая | Средняя | Высокая |
| Компиляция | Заранее | 1 раз | Каждый запуск |

---

## 🔐 Безопасность

### Изоляция скриптов

```csharp
// Ограничение времени выполнения
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await engine.ExecuteScriptAsync(code, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Script execution timed out!");
}

// Ограничение памяти (через AssemblyLoadContext - планируется)
// Ограничение доступа к API (через whitelist)
```

### Sandbox (планируется)

- [ ] Ограничение доступа к файловой системе
- [ ] Запрет на сетевые вызовы
- [ ] Ограничение на создание потоков
- [ ] Whitelist доступных типов

---

## 🛠️ Отладка

### Логирование компиляции

```csharp
try
{
    pluginManager.ScriptLoader?.CompileAndLoadScript("Plugins/Scripts/MyScript.cs");
}
catch (Exception ex)
{
    Console.WriteLine($"Compilation error: {ex.Message}");
    // Выводит ошибки компиляции с номерами строк
}
```

### Отладка скриптов

```csharp
// Проверка синтаксиса
if (!engine.ValidateSyntax(code, out var errors))
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Syntax error: {error}");
    }
}

// Пошаговое выполнение (через Continuation)
await engine.ExecuteContinuationAsync("var step1 = 10");
// Точка останова здесь
await engine.ExecuteContinuationAsync("var step2 = step1 * 2");
```

---

## 📁 Полный пример проекта

```
MyAstroProject/
├── AstroData/
│   ├── Programs/
│   │   └── Main.ast
│   ├── Registry/
│   │   ├── types.json
│   │   └── forms.json
│   └── Plugins/
│       ├── Compiled/
│       │   └── VisionPlugin.dll      # Готовый плагин
│       ├── Scripts/
│       │   ├── CustomLogic.cs        # Runtime компиляция
│       │   ├── HelloWorldScript.cs   # Runtime компиляция
│       │   └── QuickTest.csx         # C# скрипт
│       └── Cache/
│           ├── CustomLogic.dll       # Кэш компиляции
│           └── HelloWorldScript.dll  # Кэш компиляции
└── project.json
```

---

## 🚀 Быстрый старт

### 1. Создать скрипт-плагин

```bash
mkdir AstroData/Plugins/Scripts
nano AstroData/Plugins/Scripts/MyFirstScript.cs
```

### 2. Написать код

```csharp
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;

namespace AstroEditor.Plugins.Scripts;

[Plugin("MyFirstScript", "1.0", "Мой первый скрипт")]
public class MyFirstScript : IPlugin
{
    public string Name => "MyFirstScript";
    public string Version => "1.0";
    public string Description => "Мой первый скрипт";

    public void OnLoad(PluginContext context)
    {
        context.RegisterInstruction("test.hello", _ => 
            Console.WriteLine("Hello from script!"));
    }

    public void OnUnload() { }
}
```

### 3. Запустить AstroEditor

```bash
dotnet run
# Скрипт автоматически скомпилируется и загрузится
```

### 4. Использовать в программе

```
TEST.HELLO
```

---

## 🎓 Лучшие практики

1. **Кэширование** — Runtime компиляция создаёт DLL в папке Cache
2. **Версионирование** — Указывайте версию в [Plugin] атрибуте
3. **Обработка ошибок** — Всегда try-catch в плагинах
4. **Логирование** — Используйте context.Log()
5. **Тестирование** — Тестируйте скрипты перед продакшеном

---

## 🔮 Планы развития

- [ ] IntelliSense для скриптов в редакторе
- [ ] Отладчик с точками останова
- [ ] Песочница для изоляции
- [ ] Поддержка F# скриптов
- [ ] WebAssembly для браузерной версии
