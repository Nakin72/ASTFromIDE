// AstroEditor.Core.v4/Interpreter/InterpreterState.cs
using AstroEditor.Core.v4.Tables;
using AstroEditor.Core.v4.Variables;
using AstroEditor.Core.v4.Programs;

namespace AstroEditor.Core.v4.Interpreter;

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
}