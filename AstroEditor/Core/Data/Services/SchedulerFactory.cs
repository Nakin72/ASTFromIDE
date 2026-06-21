// AstroEditor.Core/Data/Services/SchedulerFactory.cs
// Фабрика планировщиков — вынесена из ProjectManager (P1-8 SRP)

using AstroEditor.Core.Binding;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Forms;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Types;
using AstroScheduler = AstroEditor.Core.Execution.TaskScheduler;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Фабрика для создания планировщиков задач.
/// Инкапсулирует логику создания TaskScheduler с правильными зависимостями.
/// </summary>
public interface ISchedulerFactory
{
    /// <summary>
    /// Создать планировщик задач.
    /// </summary>
    AstroScheduler CreateScheduler(
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programRegistry,
        DataTypeRegistry typeRegistry,
        FormRegistry formRegistry,
        Dictionary<string, Func<object?[], object?>> functions);
}

/// <summary>
/// Реализация фабрики планировщиков.
/// </summary>
public class SchedulerFactory : ISchedulerFactory
{
    private readonly IAlarmService _alarms;
    private readonly IInterruptService _interrupts;
    private readonly ITimerService _timers;
    private readonly IBindingService _bindings;

    public SchedulerFactory(
        IAlarmService alarms,
        IInterruptService interrupts,
        ITimerService timers,
        IBindingService bindings)
    {
        _alarms = alarms;
        _interrupts = interrupts;
        _timers = timers;
        _bindings = bindings;
    }

    public AstroScheduler CreateScheduler(
        VariableTableSet globalTables,
        Dictionary<string, AstroProgram> programRegistry,
        DataTypeRegistry typeRegistry,
        FormRegistry formRegistry,
        Dictionary<string, Func<object?[], object?>> functions)
    {
        return new AstroScheduler(
            globalTables,
            programRegistry,
            typeRegistry,
            formRegistry)
        {
            Functions = functions,
            InterruptService = _interrupts,
            TimerService = _timers,
            BindingService = _bindings
        };
    }
}
