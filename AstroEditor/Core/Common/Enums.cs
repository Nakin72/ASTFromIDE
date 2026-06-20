// AstroEditor/Core/Common/Enums.cs
// Все перечисления предметной области.

namespace AstroEditor.Core.Common;

// Типы данных
public enum DataTypeCategory { Core, System, Vendor, User }
public enum DataTypeKind { Primitive, Struct, Array, Alias, Enum }
public enum BuiltInPrimitive
{
    SByte, Byte, Short, UShort, Int, UInt, Long, ULong,
    Float, Double, Decimal,
    Bool, Char, String
}

// Формы
public enum FormAccessLevel { Core, System, Programmer, User }
public enum ArgumentDirection { In, Out, Ref }
public enum FieldValueType { Constant, Variable, Expression, Enum, FunctionCall, Label }

// Аварии
public enum AlarmSeverity { Info, Warning, Error, Fatal }
public enum AlarmState { Inactive, Active, Acknowledged }

// Прерывания
public enum InterruptTrigger { OnRisingEdge, OnFallingEdge, OnChange, OnValue, OnTimer, OnAlarm }
public enum InterruptExecutionMode
{
    /// <summary>Обработчик выполняется в контексте текущей программы (приостанавливая её).</summary>
    Inline,
    /// <summary>Обработчик выполняется как background-задача (параллельно).</summary>
    Background,
    /// <summary>Обработчик выполняется сразу после завершения текущей инструкции (до следующей).</summary>
    Deferred
}

// Таймеры
public enum TimerMode { Oneshot, Periodic }
