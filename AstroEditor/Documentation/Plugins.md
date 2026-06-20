# 🧩 Система плагинов AstroEditor

## 📖 Обзор

Система плагинов позволяет расширять функционал AstroEditor во время исполнения через C# плагины (аналог KAREL для FANUC).

### Возможности:
- ✅ **Динамическая загрузка** .dll из папки `Plugins/`
- ✅ **Новые инструкции** для интерпретатора
- ✅ **Встроенные функции** для выражений
- ✅ **Пользовательские типы** данных
- ✅ **Формы** для визуального редактора
- ✅ **Горячая перезагрузка** (выгрузка/загрузка)

---

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────┐
│                    AstroEditor Host                      │
│  ┌───────────────────────────────────────────────────┐  │
│  │              PluginManager.cs                      │  │
│  │  • Загрузка .dll из папки Plugins/                │  │
│  │  • Поиск классов с [PluginAttribute]              │  │
│  │  • Регистрация инструкций, форм, функций          │  │
│  └───────────────────────────────────────────────────┘  │
│                          ↑                               │
│  ┌───────────────────────┴──────────────────────────┐   │
│  │  IPlugin (интерфейс плагина)                     │   │
│  │  • OnLoad(PluginContext)                         │   │
│  │  • OnUnload()                                    │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
           ↑                    ↑                    ↑
    ┌──────┴──────┐      ┌──────┴──────┐      ┌──────┴──────┐
    │ Plugin 1    │      │ Plugin 2    │      │ Plugin 3    │
    │ (Vision)    │      │ (PLC Comms) │      │ (Math Lib)  │
    │ MyPlugin.dll│      │ Comms.dll   │      │ Math.dll    │
    └─────────────┘      └─────────────┘      └─────────────┘
```

---

## 📝 Создание плагина

### Шаг 1: Создайте проект библиотеки классов

```bash
dotnet new classlib -n MyPlugin -f net8.0
```

### Шаг 2: Добавьте ссылку на AstroEditor.Core

```bash
cd MyPlugin
dotnet add reference ../AstroEditor/AstroEditor.csproj
```

### Шаг 3: Создайте класс плагина

```csharp
using AstroEditor.Core.Plugins;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;

namespace MyPlugin;

[Plugin("MyPlugin", "1.0.0", "Описание плагина", Author = "Your Name")]
public class MyPlugin : IPlugin
{
    public string Name => "MyPlugin";
    public string Version => "1.0.0";
    public string Description => "Описание плагина";

    private PluginContext? _context;

    public void OnLoad(PluginContext context)
    {
        _context = context;

        // 1. Регистрируем новую инструкцию
        context.RegisterInstruction("myplugin.hello", ExecuteHello);

        // 2. Регистрируем функцию
        context.RegisterFunction("MY_UPPER", new UpperFunction());

        _context.Log("Info", "MyPlugin loaded successfully!");
    }

    public void OnUnload()
    {
        _context?.Log("Info", "MyPlugin unloaded");
    }

    // Обработчик инструкции
    private void ExecuteHello(Instruction instruction)
    {
        var messageField = GetFieldValue(instruction, "message");
        _context?.Log("Info", $"[HELLO] {messageField}");
        Console.WriteLine($">>> HELLO FROM PLUGIN: {messageField}");
    }

    private string GetFieldValue(Instruction instruction, string fieldName)
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && 
            field is ConstantFieldValue constField)
        {
            return constField.Value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }
}

// Пример встроенной функции
public class UpperFunction : IBuiltinFunction
{
    public object? Execute(params object?[] args)
    {
        if (args.Length == 0 || args[0] == null)
            return null;

        var input = args[0].ToString();
        return input?.ToUpper();
    }

    public int RequiredArgCount => 1;
    public bool HasVariableArgs => false;
}
```

### Шаг 4: Скомпилируйте в .dll

```bash
dotnet build -c Release
```

### Шаг 5: Скопируйте .dll в папку Plugins

```bash
cp bin/Release/net8.0/MyPlugin.dll /path/to/project/Plugins/
```

---

## 🔧 API плагина

### PluginContext

| Метод/Свойство | Описание |
|----------------|----------|
| `RegisterInstruction(id, handler)` | Регистрация обработчика инструкции |
| `RegisterFunction(name, function)` | Регистрация встроенной функции |
| `RegisterForm(form)` | Регистрация формы для редактора |
| `RegisterType(type)` | Регистрация типа данных |
| `Log(level, message)` | Логирование от плагина |

### IPlugin

| Метод/Свойство | Описание |
|----------------|----------|
| `Name` | Имя плагина |
| `Version` | Версия плагина |
| `Description` | Описание плагина |
| `OnLoad(context)` | Вызывается при загрузке |
| `OnUnload()` | Вызывается при выгрузке |

### PluginAttribute

```csharp
[Plugin(
    name: "MyPlugin",
    version: "1.0.0", 
    description: "Описание",
    Author = "Your Name",
    MinHostVersion = "4.0"
)]
```

---

## 🎯 Примеры использования

### 1. Инструкция для работы с файлами

```csharp
[InstructionHandler("file.write")]
private void ExecuteFileWrite(Instruction instruction)
{
    var path = GetFieldValue(instruction, "path");
    var content = GetFieldValue(instruction, "content");
    File.WriteAllText(path, content);
}
```

### 2. Функция для работы с JSON

```csharp
public class JsonParseFunction : IBuiltinFunction
{
    public object? Execute(params object?[] args)
    {
        var json = args[0]?.ToString();
        return JsonSerializer.Deserialize<object>(json);
    }

    public int RequiredArgCount => 1;
    public bool HasVariableArgs => false;
}
```

### 3. Функция для отправки HTTP запросов

```csharp
public class HttpGetFunction : IBuiltinFunction
{
    private readonly HttpClient _client = new();

    public async Task<object?> ExecuteAsync(params object?[] args)
    {
        var url = args[0]?.ToString();
        var response = await _client.GetStringAsync(url);
        return response;
    }

    public object? Execute(params object?[] args)
    {
        return ExecuteAsync(args).Result;
    }

    public int RequiredArgCount => 1;
    public bool HasVariableArgs => false;
}
```

---

## 🔐 Безопасность

### Изоляция плагинов
- Плагины загружаются в отдельный `AssemblyLoadContext` (планируется)
- Ограничение на выполнение опасных операций (TODO)
- Проверка подписи плагинов (TODO)

### Песочница
Для повышения безопасности можно ограничить доступ плагинов к:
- Файловой системе
- Сети
- Реестру
- Процессам

---

## 📊 Отладка плагинов

### Логирование
```csharp
_context.Log("Info", "Сообщение");
_context.Log("Warning", "Предупреждение");
_context.Log("Error", "Ошибка");
```

### Вывод в консоль
```csharp
Console.WriteLine($"[MyPlugin] {message}");
```

---

## 🚀 Горячая перезагрузка

```csharp
// Выгрузить плагин
projectManager.Plugins?.UnloadPlugin("MyPlugin");

// Загрузить заново
projectManager.Plugins?.LoadPlugin("Plugins/MyPlugin.dll");
```

---

## 📁 Структура проекта с плагинами

```
MyProject/
├── AstroData/
│   ├── Programs/
│   │   └── Main.ast
│   ├── Registry/
│   │   ├── types.json
│   │   ├── forms.json
│   │   └── globals.json
│   └── Plugins/
│       ├── MyPlugin.dll
│       └── AnotherPlugin.dll
└── project.json
```

---

## 🎓 Лучшие практики

1. **Минимальные зависимости** — плагин должен зависеть только от AstroEditor.Core
2. **Быстрая загрузка** — OnLoad() должен выполняться быстро
3. **Обработка ошибок** — всегда обрабатывайте исключения в плагине
4. **Логирование** — используйте context.Log() для отладки
5. **Документация** — документируйте API вашего плагина

---

## 🔮 Планы развития

- [ ] AssemblyLoadContext для изоляции
- [ ] Подпись плагинов
- [ ] Асинхронные обработчики
- [ ] UI расширения для редактора
- [ ] Marketplace плагинов
