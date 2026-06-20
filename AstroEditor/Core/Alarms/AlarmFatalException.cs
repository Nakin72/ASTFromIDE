// AstroEditor.Core/Alarms/AlarmFatalException.cs
// Исключение, выбрасываемое при фатальной аварии

namespace AstroEditor.Core.Alarms;

/// <summary>
/// Фатальная авария — выполнение программы прерывается.
/// </summary>
public class AlarmFatalException : Exception
{
    public int AlarmCode { get; }
    public string AlarmName { get; }

    public AlarmFatalException(int code, string name, string message)
        : base($"FATAL ALARM #{code} {name}: {message}")
    {
        AlarmCode = code;
        AlarmName = name;
    }
}
