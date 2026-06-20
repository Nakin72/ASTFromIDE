// AstroEditor/Plugins/Scripts/QuickScript.csx
// Пример C# скрипта для интерактивного выполнения
// Запускается через CSharpScriptEngine.ExecuteScript()

using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;

// Скрипт имеет доступ ко всем пространствам имен AstroEditor

// Пример 1: Простое вычисление
var result = 10 + 20 * 3;
Console.WriteLine($"Результат: {result}");

// Пример 2: Работа с переменными
var counter = 0;
counter++;
counter++;
Console.WriteLine($"Counter: {counter}");

// Пример 3: Использование LINQ
var numbers = new[] { 1, 2, 3, 4, 5 };
var sum = numbers.Sum();
Console.WriteLine($"Sum: {sum}");

// Пример 4: Создание и регистрация функции
// (в реальном использовании через PluginContext)
Console.WriteLine("Script executed successfully!");

// Возвращаемое значение
return result + sum;
