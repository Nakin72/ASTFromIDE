// AstroEditor.Core/Alarms/AlarmManager.cs
// Центральный менеджер аварий. Реестр определений + активные + история.

using AstroEditor.Core.Common;
using AstroEditor.Core.Execution;

namespace AstroEditor.Core.Alarms;

/// <summary>
/// Управляет определениями аварий, активными экземплярами и историей.
/// </summary>
public class AlarmManager : IAlarmService
{
    private readonly Dictionary<int, AlarmDefinition> _definitions = new();
    private readonly Dictionary<int, AlarmInstance> _activeAlarms = new();
    private readonly List<AlarmInstance> _alarmHistory = new();
    private readonly object _lock = new();

    // ========== События ==========

    /// <summary>Авария активирована.</summary>
    public event Action<AlarmInstance>? OnAlarmRaised;

    /// <summary>Авария квитирована оператором.</summary>
    public event Action<AlarmInstance>? OnAlarmAcknowledged;

    /// <summary>Авария сброшена.</summary>
    public event Action<AlarmInstance>? OnAlarmCleared;

    /// <summary>Все активные аварии сброшены.</summary>
    public event Action? OnAllAlarmsCleared;

    // ========== Определения ==========

    public IReadOnlyDictionary<int, AlarmDefinition> Definitions => _definitions;

    /// <summary>Зарегистрировать определение аварии.</summary>
    public void RegisterAlarm(AlarmDefinition definition)
    {
        lock (_lock)
        {
            _definitions[definition.Code] = definition;
        }
    }

    /// <summary>Получить определение по коду.</summary>
    public AlarmDefinition? GetDefinition(int code)
    {
        lock (_lock) { return _definitions.GetValueOrDefault(code); }
    }

    /// <summary>Удалить определение (если нет активных экземпляров).</summary>
    public bool UnregisterAlarm(int code)
    {
        lock (_lock)
        {
            if (_activeAlarms.ContainsKey(code)) return false;
            return _definitions.Remove(code);
        }
    }

    /// <summary>Создать пользовательскую аварию.</summary>
    public AlarmDefinition CreateUserAlarm(string name, string message, AlarmSeverity severity = AlarmSeverity.Error)
    {
        var code = GenerateCode();
        var def = new AlarmDefinition
        {
            Code = code,
            Category = DataTypeCategory.User,
            Severity = severity,
            Name = name,
            MessageTemplate = message,
            RequiresAcknowledge = true,
            AutoReset = false
        };
        RegisterAlarm(def);
        return def;
    }

    // ========== Активные аварии ==========

    public IReadOnlyDictionary<int, AlarmInstance> ActiveAlarms => _activeAlarms;
    public IReadOnlyList<AlarmInstance> AlarmHistory => _alarmHistory.AsReadOnly();

    /// <summary>Поднять аварию. Возвращает экземпляр.</summary>
    public AlarmInstance Raise(int code, params object?[] args)
    {
        var def = GetDefinition(code);
        if (def == null)
            throw new KeyNotFoundException($"Alarm code {code} not registered");

        lock (_lock)
        {
            // Если уже активна — увеличиваем счётчик
            if (_activeAlarms.TryGetValue(code, out var existing))
            {
                existing.OccurrenceCount++;
                existing.RaisedAt = DateTime.UtcNow; // обновляем время
                return existing;
            }

            var instance = new AlarmInstance
            {
                Code = code,
                State = AlarmState.Active,
                RaisedAt = DateTime.UtcNow,
                Arguments = args.ToList(),
                OccurrenceCount = 1
            };

            _activeAlarms[code] = instance;
            _alarmHistory.Add(instance.Clone());

            System.Diagnostics.Debug.WriteLine($"[ALARM] {def.Severity} #{code} {def.Name}: {def.FormatMessage(args)}");

            OnAlarmRaised?.Invoke(instance);

            return instance;
        }
    }

    /// <summary>Поднять аварию с привязкой к строке программы.</summary>
    public AlarmInstance RaiseFromProgram(int code, string? programName, int? lineNumber, params object?[] args)
    {
        var instance = Raise(code, args);
        instance.SourceLine = lineNumber;
        instance.SourceProgram = programName;
        return instance;
    }

    /// <summary>Квитировать аварию.</summary>
    public bool Acknowledge(int code)
    {
        lock (_lock)
        {
            if (!_activeAlarms.TryGetValue(code, out var instance))
                return false;

            instance.State = AlarmState.Acknowledged;
            instance.AcknowledgedAt = DateTime.UtcNow;
            OnAlarmAcknowledged?.Invoke(instance);
            return true;
        }
    }

    /// <summary>Квитировать все активные аварии.</summary>
    public int AcknowledgeAll()
    {
        int count = 0;
        lock (_lock)
        {
            foreach (var code in _activeAlarms.Keys.ToList())
            {
                if (Acknowledge(code)) count++;
            }
        }
        return count;
    }

    /// <summary>Сбросить аварию (перевести в Inactive).</summary>
    public bool Clear(int code)
    {
        lock (_lock)
        {
            if (!_activeAlarms.TryGetValue(code, out var instance))
                return false;

            instance.State = AlarmState.Inactive;
            instance.ClearedAt = DateTime.UtcNow;

            // Копируем в историю финальное состояние
            _alarmHistory.Add(instance.Clone());
            _activeAlarms.Remove(code);

            OnAlarmCleared?.Invoke(instance);
            return true;
        }
    }

    /// <summary>Сбросить все аварии.</summary>
    public int ClearAll()
    {
        int count = 0;
        lock (_lock)
        {
            foreach (var code in _activeAlarms.Keys.ToList())
            {
                if (Clear(code)) count++;
            }
        }
        OnAllAlarmsCleared?.Invoke();
        return count;
    }

    /// <summary>Есть ли активные аварии?</summary>
    public bool HasActiveAlarms
    {
        get { lock (_lock) { return _activeAlarms.Count > 0; } }
    }

    /// <summary>Количество активных аварий заданной тяжести.</summary>
    public int CountActive(AlarmSeverity? severity = null)
    {
        lock (_lock)
        {
            if (severity == null) return _activeAlarms.Count;
            return _activeAlarms.Values.Count(a =>
            {
                var def = GetDefinition(a.Code);
                return def != null && def.Severity == severity;
            });
        }
    }

    /// <summary>Очистить историю.</summary>
    public void ClearHistory()
    {
        lock (_lock) { _alarmHistory.Clear(); }
    }

    /// <summary>Проверить, является ли авария фатальной (останавливает программу).</summary>
    public bool IsFatal(int code)
    {
        var def = GetDefinition(code);
        return def?.Fatal ?? false;
    }

    // ========== Внутреннее ==========

    private int _nextCode = 1000;
    private int GenerateCode()
    {
        lock (_lock)
        {
            // Пользовательские коды: 1000+, системные: <1000
            while (_definitions.ContainsKey(_nextCode) || _activeAlarms.ContainsKey(_nextCode))
                _nextCode++;
            return _nextCode++;
        }
    }

    /// <summary>Создать системные аварии (по умолчанию).</summary>
    public void RegisterSystemAlarms()
    {
        RegisterAlarm(new AlarmDefinition
        {
            Code = 1,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Error,
            Name = "DIVISION_BY_ZERO",
            MessageTemplate = "Деление на ноль",
            Description = "Попытка деления на ноль",
            Fatal = true,
            RequiresAcknowledge = false,
            AutoReset = false
        });

        RegisterAlarm(new AlarmDefinition
        {
            Code = 2,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Error,
            Name = "VARIABLE_NOT_FOUND",
            MessageTemplate = "Переменная '{0}' не найдена",
            Description = "Обращение к несуществующей переменной",
            Fatal = true,
            RequiresAcknowledge = false,
            AutoReset = false
        });

        RegisterAlarm(new AlarmDefinition
        {
            Code = 3,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Warning,
            Name = "TYPE_MISMATCH",
            MessageTemplate = "Несовместимость типов: {0}",
            Description = "Попытка присвоить значение несовместимого типа",
            Fatal = false,
            RequiresAcknowledge = false,
            AutoReset = true
        });

        RegisterAlarm(new AlarmDefinition
        {
            Code = 4,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Error,
            Name = "PROGRAM_NOT_FOUND",
            MessageTemplate = "Программа '{0}' не найдена",
            Fatal = true,
            RequiresAcknowledge = false
        });

        RegisterAlarm(new AlarmDefinition
        {
            Code = 5,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Warning,
            Name = "MAX_CYCLES_EXCEEDED",
            MessageTemplate = "Превышено максимальное число циклов ({0})",
            Description = "Программа превысила лимит итераций",
            Fatal = true,
            RequiresAcknowledge = false
        });

        RegisterAlarm(new AlarmDefinition
        {
            Code = 10,
            Category = DataTypeCategory.Core,
            Severity = AlarmSeverity.Error,
            Name = "USER_DEFINED",
            MessageTemplate = "{0}",
            Description = "Пользовательская авария, созданная через core.alarm.raise",
            Fatal = false,
            RequiresAcknowledge = true,
            AutoReset = true
        });
    }
}
