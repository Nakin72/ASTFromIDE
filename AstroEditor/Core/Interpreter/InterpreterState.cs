// AstroEditor.Core.v4/Interpreter/InterpreterState.cs
using AstroEditor.Core.Tables;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Interpreter;

/// <summary>
/// Состояние выполнения программы
/// </summary>
public class InterpreterState
{
    /// <summary>Текущий номер строки (индекс в списке инструкций)</summary>
    public int CurrentLineIndex { get; set; }

    /// <summary>Стек вызовов (для возврата из подпрограмм)</summary>
    public Stack<CallFrame> CallStack { get; set; } = new();

    /// <summary>Стек контекстов циклов (для Break/Continue)</summary>
    public Stack<LoopContext> LoopStack { get; set; } = new();

    /// <summary>Флаг, что выполнение должно остановиться</summary>
    public bool StopRequested { get; set; }

    /// <summary>Флаг паузы (для пошагового режима)</summary>
    public bool PauseRequested { get; set; }

    /// <summary>Текущий контекст локальных таблиц (может меняться при вызове)</summary>
    public VariableTableSet CurrentLocalTables { get; set; } = null!;

    /// <summary>Ссылка на выполняемую программу</summary>
    public AstroProgram Program { get; set; } = null!;

    /// <summary>Счётчик итераций текущего цикла (для ограничения)</summary>
    public int CurrentLoopIteration { get; set; }

    /// <summary>Результат выполнения программы (если функция)</summary>
    public object? ReturnValue { get; set; }
    public Stack<SwitchContext> SwitchStack { get; set; } = new();

    /// <summary>Стек контекстов обработки исключений (для TRY/CATCH)</summary>
    public Stack<ExceptionContext> ExceptionStack { get; set; } = new();

    /// <summary>Текущее активное исключение (если есть)</summary>
    public ExceptionContext? CurrentException { get; set; }
}
public class SwitchContext
{
    public object? ExpressionValue { get; set; }
    public int EndLineIndex { get; set; }
    public bool IsExecuting { get; set; } // true, если мы уже внутри выполняемой ветки
}
/// <summary>
/// Фрейм вызова (информация о вызванной программе)
/// </summary>
public class CallFrame
{
    public AstroProgram Program { get; set; } = null!;
    public int ReturnLineIndex { get; set; } // строка, на которую вернуться после завершения
    public VariableTableSet LocalTables { get; set; } = null!;
    public Dictionary<string, object?> Arguments { get; set; } = new(); // переданные аргументы
}

/// <summary>
/// Контекст цикла (для поддержки Break и Continue)
/// </summary>
// AstroEditor.Core.v4/Interpreter/InterpreterState.cs
public class LoopContext
{
    public int StartLineIndex { get; set; }
    public int EndLineIndex { get; set; }
    public int MaxIterations { get; set; } = 1000;
    public int CurrentIteration { get; set; }

    // Специфичные для FOR
    public bool IsForLoop { get; set; }
    public string? VariableName { get; set; } // имя переменной-счётчика
    public object? StartValue { get; set; }
    public object? EndValue { get; set; }
    public object? StepValue { get; set; }
    public bool IsIncreasing { get; set; } // true если шаг > 0
    public object? CurrentValue { get; set; }
    
    // Специфичные для FOR EACH
    public bool IsForEachLoop { get; set; }
    public string? ItemVariableName { get; set; } // имя переменной для элемента
    public string? CollectionVariableName { get; set; } // имя переменной коллекции
    public List<object?>? CollectionValues { get; set; } // значения коллекции
    public int CurrentIndex { get; set; } // текущий индекс в коллекции
}

/// <summary>
/// Контекст обработки исключений (для TRY/CATCH/FINALLY)
/// </summary>
public class ExceptionContext
{
    /// <summary>Индекс строки начала TRY блока</summary>
    public int TryStartLineIndex { get; set; }

    /// <summary>Индекс строки начала CATCH блока (если есть)</summary>
    public int? CatchLineIndex { get; set; }

    /// <summary>Индекс строки начала FINALLY блока (если есть)</summary>
    public int? FinallyLineIndex { get; set; }

    /// <summary>Индекс строки после ENDTRY</summary>
    public int EndLineIndex { get; set; }

    /// <summary>Переменная для хранения исключения (если указана в CATCH)</summary>
    public string? ExceptionVariableName { get; set; }

    /// <summary>Код ошибки для фильтрации (0 = любая ошибка)</summary>
    public int ErrorCodeFilter { get; set; } = 0;

    /// <summary>Флаг: исключение было перехвачено</summary>
    public bool ExceptionCaught { get; set; } = false;

    /// <summary>Флаг: выполнение блока FINALLY</summary>
    public bool FinallyExecuted { get; set; } = false;

    /// <summary>Сообщение об исключении</summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>Код исключения</summary>
    public int ExceptionCode { get; set; }
}