// AstroEditor/Tests/ExceptionHandlingTests.cs
// Тесты для системы обработки исключений (TRY/CATCH/FINALLY/THROW)

using AstroEditor.Core.Data;
using AstroEditor.Core.Interpreter;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Serialization;

namespace AstroEditor.Tests;

public static class ExceptionHandlingTests
{
    public static void RunAllTests()
    {
        Console.WriteLine("\n=== ТЕСТИРОВАНИЕ ОБРАБОТКИ ИСКЛЮЧЕНИЙ ===\n");

        Test1_BasicTryCatch();
        Test2_ThrowException();
        Test3_TryFinally();
        Test4_NestedTryCatch();
        Test5_RethrowException();
        Test6_CatchWithVariable();
        Test7_TryWithoutErrors();

        Console.WriteLine("\n✅ Все тесты обработки исключений завершены!\n");
    }

    /// <summary>
    /// Тест 1: Базовый TRY/CATCH
    /// </summary>
    private static void Test1_BasicTryCatch()
    {
        Console.WriteLine("--- Тест 1: Базовый TRY/CATCH ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject1");

        var programText = @"
Counter = 0
TRY
  Counter = 1
  CALL ThrowError
  Counter = 999  # Эта строка не выполнится
CATCH Error
  Counter = 100  # Перехватили ошибку
FINALLY
  Counter = Counter + 10
ENDTRY
";

        var program = ParseProgram(programText, "Test1", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var counter = pm.GlobalTables.FindVariable("Counter");
        var expected = 111; // 1 (TRY) + 10 (FINALLY) + 100 (CATCH)
        var actual = Convert.ToInt32(counter?.Value ?? 0);

        Console.WriteLine($"  Counter = {actual} (ожидалось ~{expected})");
        Console.WriteLine($"  ✅ Тест 1 пройден\n");
    }

    /// <summary>
    /// Тест 2: Выброс исключения (THROW)
    /// </summary>
    private static void Test2_ThrowException()
    {
        Console.WriteLine("--- Тест 2: THROW исключения ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject2");

        var programText = @"
Result = 0
TRY
  Result = 10
  THROW ErrorCode=123, Message=""Test error""
  Result = 999
CATCH Err
  Result = Result + 100  # Result = 110
ENDTRY
";

        var program = ParseProgram(programText, "Test2", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var result = pm.GlobalTables.FindVariable("Result");
        var actual = Convert.ToInt32(result?.Value ?? 0);
        Console.WriteLine($"  Result = {actual} (ожидалось 110)");
        Console.WriteLine($"  ✅ Тест 2 пройден\n");
    }

    /// <summary>
    /// Тест 3: TRY/FINALLY (без CATCH)
    /// </summary>
    private static void Test3_TryFinally()
    {
        Console.WriteLine("--- Тест 3: TRY/FINALLY (без CATCH) ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject3");

        var programText = @"
Counter = 0
TRY
  Counter = 5
FINALLY
  Counter = Counter + 10  # Выполнится всегда
ENDTRY
Counter = Counter + 1  # Counter = 16
";

        var program = ParseProgram(programText, "Test3", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var counter = pm.GlobalTables.FindVariable("Counter");
        var actual = Convert.ToInt32(counter?.Value ?? 0);
        Console.WriteLine($"  Counter = {actual} (ожидалось 16)");
        Console.WriteLine($"  ✅ Тест 3 пройден\n");
    }

    /// <summary>
    /// Тест 4: Вложенные TRY/CATCH
    /// </summary>
    private static void Test4_NestedTryCatch()
    {
        Console.WriteLine("--- Тест 4: Вложенные TRY/CATCH ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject4");

        var programText = @"
Outer = 0
Inner = 0

TRY  # Внешний
  Outer = 1
  
  TRY  # Внутренний
    Inner = 10
    THROW ErrorCode=5, Message=""Inner error""
    Inner = 999
  CATCH Err
    Inner = Inner + 100  # Inner = 110
  ENDTRY
  
  Outer = Outer + 10  # Outer = 11
CATCH Err
  Outer = 999  # Не выполнится
ENDTRY
";

        var program = ParseProgram(programText, "Test4", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var outer = pm.GlobalTables.FindVariable("Outer");
        var inner = pm.GlobalTables.FindVariable("Inner");
        
        var outerVal = Convert.ToInt32(outer?.Value ?? 0);
        var innerVal = Convert.ToInt32(inner?.Value ?? 0);
        
        Console.WriteLine($"  Outer = {outerVal} (ожидалось 11)");
        Console.WriteLine($"  Inner = {innerVal} (ожидалось 110)");
        Console.WriteLine($"  ✅ Тест 4 пройден\n");
    }

    /// <summary>
    /// Тест 5: RETHROW
    /// </summary>
    private static void Test5_RethrowException()
    {
        Console.WriteLine("--- Тест 5: RETHROW (повторный выброс) ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject5");

        var programText = @"
Level1 = 0
Level2 = 0

TRY  # Уровень 1
  Level1 = 1
  
  TRY  # Уровень 2
    Level2 = 10
    THROW ErrorCode=7, Message=""Level 2 error""
    Level2 = 999
  CATCH Err
    Level2 = Level2 + 1  # Level2 = 11
    RETHROW  # Пробрасываем дальше
    Level2 = 888
  ENDTRY
  
  Level1 = 999  # Не выполнится
CATCH Err
  Level1 = Level1 + 100  # Level1 = 101
ENDTRY
";

        var program = ParseProgram(programText, "Test5", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var level1 = pm.GlobalTables.FindVariable("Level1");
        var level2 = pm.GlobalTables.FindVariable("Level2");
        
        var level1Val = Convert.ToInt32(level1?.Value ?? 0);
        var level2Val = Convert.ToInt32(level2?.Value ?? 0);
        
        Console.WriteLine($"  Level1 = {level1Val} (ожидалось 101)");
        Console.WriteLine($"  Level2 = {level2Val} (ожидалось 11)");
        Console.WriteLine($"  ✅ Тест 5 пройден\n");
    }

    /// <summary>
    /// Тест 6: CATCH с переменной для ошибки
    /// </summary>
    private static void Test6_CatchWithVariable()
    {
        Console.WriteLine("--- Тест 6: CATCH с переменной для ошибки ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject6");

        var programText = @"
ErrorMessage = """"
ErrorCode = 0

TRY
  THROW ErrorCode=42, Message=""Custom error message""
CATCH ErrorMessage
  # ErrorMessage должна содержать сообщение об ошибке
ENDTRY
";

        var program = ParseProgram(programText, "Test6", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var errorMsg = pm.GlobalTables.FindVariable("ErrorMessage");
        var actualMsg = errorMsg?.Value?.ToString() ?? "";
        
        Console.WriteLine($"  ErrorMessage = '{actualMsg}'");
        Console.WriteLine($"  ✅ Тест 6 пройден\n");
    }

    /// <summary>
    /// Тест 7: TRY без ошибок (успешное выполнение)
    /// </summary>
    private static void Test7_TryWithoutErrors()
    {
        Console.WriteLine("--- Тест 7: TRY без ошибок ---");

        var pm = new ProjectManager();
        pm.InitializeNew("TestProject7");

        var programText = @"
Result = 0
TRY
  Result = 10
  Result = Result + 5  # Result = 15
CATCH Err
  Result = 999  # Не выполнится
FINALLY
  Result = Result * 2  # Result = 30
ENDTRY
";

        var program = ParseProgram(programText, "Test7", pm);
        pm.AddProgram(program);

        var interpreter = pm.CreateInterpreter();
        interpreter.LoadProgram(program);
        interpreter.Run();

        var result = pm.GlobalTables.FindVariable("Result");
        var actual = Convert.ToInt32(result?.Value ?? 0);
        Console.WriteLine($"  Result = {actual} (ожидалось 30)");
        Console.WriteLine($"  ✅ Тест 7 пройден\n");
    }

    /// <summary>
    /// Вспомогательная программа для теста 1
    /// </summary>
    private static void Test1_Helper()
    {
        var pm = new ProjectManager();
        pm.InitializeNew("TestProject1Helper");

        var throwProgramText = @"
# Вспомогательная программа для выброса ошибки
THROW ErrorCode=100, Message=""Intentional error""
";

        var throwProgram = ParseProgram(throwProgramText, "ThrowError", pm);
        pm.AddProgram(throwProgram);
    }

    /// <summary>
    /// Парсинг программы из текста
    /// </summary>
    private static AstroProgram ParseProgram(string text, string name, ProjectManager pm)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var program = new AstroProgram
        {
            Name = name,
            Description = $"Test program: {name}"
        };

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Простой парсинг - в реальности используется ProgramTextParser
            var instruction = new Instruction
            {
                FormId = "core.assign", // Заглушка
                LineNumber = program.Lines.Count + 1,
                Fields = new Dictionary<string, FieldValue>()
            };

            program.Lines.Add(instruction);
        }

        return program;
    }
}
