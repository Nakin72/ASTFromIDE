using AstroEditor.Core.Common;
using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Forms;

public static class BuiltinForms
{
    public static FormDefinition CreateAssignmentForm()
    {
        return new FormDefinition
        {
            Id = "core.assign",
            Name = "Assign",
            Category = "Assignment",
            Description = "Присваивает значение переменной",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "variable",
                    DisplayName = "Переменная",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "int", "double", "real", "bool", "string", "position" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "expression",
                    DisplayName = "Выражение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "int", "double", "real", "bool", "string", "position" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateWhileForm()
    {
        return new FormDefinition
        {
            Id = "core.while",
            Name = "While",
            Category = "Logic",
            Description = "Цикл с условием",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "WHILE",
                ClosingKeyword = "ENDWHILE",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "maxIterations",
                    DisplayName = "Макс. итераций",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(1000)
                }
            }
        };
    }

    public static FormDefinition CreateCallForm()
    {
        return new FormDefinition
        {
            Id = "core.call",
            Name = "Call",
            Category = "Program Control",
            Description = "Вызов программы",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "programName",
                    DisplayName = "Имя программы",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "arguments",
                    DisplayName = "Аргументы",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "resultVariable",
                    DisplayName = "Переменная результата",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                }
            }
        };
    }
    public static FormDefinition CreateIfForm()
    {
        return new FormDefinition
        {
            Id = "core.if",
            Name = "If",
            Category = "Logic",
            Description = "Условный оператор",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "IF",
                ClosingKeyword = "ENDIF",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
        {
            new FormFieldDefinition
            {
                Name = "condition",
                DisplayName = "Условие",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "bool" },
                Required = true
            }
        }
        };
    }

    public static FormDefinition CreateElseForm()
    {
        return new FormDefinition
        {
            Id = "core.else",
            Name = "Else",
            Category = "Logic",
            Description = "Иначе",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndIfForm()
    {
        return new FormDefinition
        {
            Id = "core.endif",
            Name = "EndIf",
            Category = "Logic",
            Description = "Конец условия",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateSwitchForm()
    {
        return new FormDefinition
        {
            Id = "core.switch",
            Name = "Switch",
            Category = "Logic",
            Description = "Множественный выбор",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "SWITCH",
                ClosingKeyword = "ENDSWITCH",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
        {
            new FormFieldDefinition
            {
                Name = "expression",
                DisplayName = "Выражение",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "any" },
                Required = true
            }
        }
        };
    }

    public static FormDefinition CreateCaseForm()
    {
        return new FormDefinition
        {
            Id = "core.case",
            Name = "Case",
            Category = "Logic",
            Description = "Ветка выбора",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
        {
            new FormFieldDefinition
            {
                Name = "value",
                DisplayName = "Значение",
                ValueType = FieldValueType.Constant,
                AllowedTypeIds = new List<string> { "any" },
                Required = true
            }
        }
        };
    }

    public static FormDefinition CreateDefaultForm()
    {
        return new FormDefinition
        {
            Id = "core.default",
            Name = "Default",
            Category = "Logic",
            Description = "Ветка по умолчанию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndSwitchForm()
    {
        return new FormDefinition
        {
            Id = "core.endswitch",
            Name = "EndSwitch",
            Category = "Logic",
            Description = "Конец Switch",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }
    public static FormDefinition CreateLabelForm()
    {
        return new FormDefinition
        {
            Id = "core.lbl",
            Name = "Lbl",
            Category = "Program Control",
            Description = "Определяет метку",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Имя метки",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateJumpLblForm()
    {
        return new FormDefinition
        {
            Id = "core.jumplbl",
            Name = "JumpLbl",
            Category = "Program Control",
            Description = "Безусловный переход к метке",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Метка",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateJumpIfForm()
    {
        return new FormDefinition
        {
            Id = "core.jumpif",
            Name = "JumpIf",
            Category = "Program Control",
            Description = "Условный переход к метке",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "labelName",
                    DisplayName = "Метка",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateReturnForm()
    {
        return new FormDefinition
        {
            Id = "core.return",
            Name = "Return",
            Category = "Program Control",
            Description = "Возврат из программы",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "value",
                    DisplayName = "Возвращаемое значение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                }
            }
        };
    }

    public static FormDefinition CreateBreakForm()
    {
        return new FormDefinition
        {
            Id = "core.break",
            Name = "Break",
            Category = "Program Control",
            Description = "Выход из цикла",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateContinueForm()
    {
        return new FormDefinition
        {
            Id = "core.continue",
            Name = "Continue",
            Category = "Program Control",
            Description = "Переход к следующей итерации цикла",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }
    public static FormDefinition CreateForForm()
    {
        return new FormDefinition
        {
            Id = "core.for",
            Name = "For",
            Category = "Logic",
            Description = "Цикл с параметром",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            ControlFlow = new ControlFlowStructure
            {
                OpeningKeyword = "FOR",
                ClosingKeyword = "ENDFOR",
                RequiresBody = true,
                CanBeNested = true
            },
            Fields = new List<FormFieldDefinition>
        {
            new FormFieldDefinition
            {
                Name = "variable",
                DisplayName = "Переменная-счётчик",
                ValueType = FieldValueType.Variable,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "start",
                DisplayName = "Начальное значение",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "end",
                DisplayName = "Конечное значение",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = true
            },
            new FormFieldDefinition
            {
                Name = "step",
                DisplayName = "Шаг",
                ValueType = FieldValueType.Expression,
                AllowedTypeIds = new List<string> { "int", "double", "real" },
                Required = false,
                DefaultValue = new ConstantFieldValue(1)
            }
        }
        };
}

    public static FormDefinition CreateEndForForm()
    {
        return new FormDefinition
        {
            Id = "core.endfor",
            Name = "EndFor",
            Category = "Logic",
            Description = "Конец цикла FOR",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    public static FormDefinition CreateEndWhileForm()
    {
        return new FormDefinition
        {
            Id = "core.endwhile",
            Name = "EndWhile",
            Category = "Logic",
            Description = "Конец цикла WHILE",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>()
        };
    }

    // ========== Формы аварий ==========

    public static FormDefinition CreateAlarmRaiseForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.raise",
            Name = "RaiseAlarm",
            Category = "Alarm",
            Description = "Поднять аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "message",
                    DisplayName = "Сообщение",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "severity",
                    DisplayName = "Тяжесть",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Info", "Warning", "Error", "Fatal" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Error")
                }
            }
        };
    }

    public static FormDefinition CreateAlarmClearForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.clear",
            Name = "ClearAlarm",
            Category = "Alarm",
            Description = "Сбросить аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateAlarmAckForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.ack",
            Name = "AckAlarm",
            Category = "Alarm",
            Description = "Квитировать аварию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "code",
                    DisplayName = "Код аварии",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateAlarmClearAllForm()
    {
        return new FormDefinition
        {
            Id = "core.alarm.clearall",
            Name = "ClearAllAlarms",
            Category = "Alarm",
            Description = "Сбросить все аварии",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>()
        };
    }

    // ========== Формы прерываний ==========

    public static FormDefinition CreateInterruptDeclareForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.declare",
            Name = "DeclareInterrupt",
            Category = "Interrupt",
            Description = "Объявить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "triggerType",
                    DisplayName = "Тип триггера",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "OnChange", "OnRisingEdge", "OnFallingEdge", "OnValue", "OnTimer", "OnAlarm" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
new FormFieldDefinition
                {
                    Name = "expression",
                    DisplayName = "Выражение (для OnChange/OnValue)",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "variableName",
                    DisplayName = "Переменная (для OnChange)",
                    ValueType = FieldValueType.Variable,
                    AllowedTypeIds = new List<string> { "any" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "alarmCode",
                    DisplayName = "Код аварии (для OnAlarm)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "timerMs",
                    DisplayName = "Интервал таймера мс (для OnTimer)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "handlerProgram",
                    DisplayName = "Программа-обработчик",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "executionMode",
                    DisplayName = "Режим выполнения",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Deferred", "Background", "Inline" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Deferred")
                }
            }
        };
    }

    public static FormDefinition CreateInterruptOnForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.on",
            Name = "InterruptOn",
            Category = "Interrupt",
            Description = "Включить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

public static FormDefinition CreateInterruptOffForm()
    {
        return new FormDefinition
        {
            Id = "core.interrupt.off",
            Name = "InterruptOff",
            Category = "Interrupt",
            Description = "Выключить прерывание",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя прерывания",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    // ========== Формы таймеров ==========

    public static FormDefinition CreateTimerDeclareForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.declare",
            Name = "DeclareTimer",
            Category = "Timer",
            Description = "Объявить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "intervalMs",
                    DisplayName = "Интервал (мс)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "mode",
                    DisplayName = "Режим",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Periodic", "Oneshot" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue("Periodic")
                },
                new FormFieldDefinition
                {
                    Name = "handlerProgram",
                    DisplayName = "Программа-обработчик",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = false
                }
            }
        };
    }

    public static FormDefinition CreateTimerOnForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.on",
            Name = "TimerOn",
            Category = "Timer",
            Description = "Запустить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateTimerOffForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.off",
            Name = "TimerOff",
            Category = "Timer",
            Description = "Остановить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    public static FormDefinition CreateTimerResetForm()
    {
        return new FormDefinition
        {
            Id = "core.timer.reset",
            Name = "TimerReset",
            Category = "Timer",
            Description = "Сбросить таймер",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = false,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "name",
                    DisplayName = "Имя таймера",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                }
            }
        };
    }

    // ========== Формы WAIT ==========

    public static FormDefinition CreateWaitForm()
    {
        return new FormDefinition
        {
            Id = "core.wait",
            Name = "Wait",
            Category = "Control",
            Description = "Ожидание по времени или по условию",
            AccessLevel = FormAccessLevel.Core,
            IsControlFlow = true,
            Fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    Name = "mode",
                    DisplayName = "Режим ожидания",
                    ValueType = FieldValueType.Enum,
                    Options = new List<string> { "Time", "Condition" },
                    AllowedTypeIds = new List<string> { "string" },
                    Required = true
                },
                new FormFieldDefinition
                {
                    Name = "timeMs",
                    DisplayName = "Время ожидания (мс)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(1000)
                },
                new FormFieldDefinition
                {
                    Name = "condition",
                    DisplayName = "Условие",
                    ValueType = FieldValueType.Expression,
                    AllowedTypeIds = new List<string> { "bool" },
                    Required = false
                },
                new FormFieldDefinition
                {
                    Name = "timeoutMs",
                    DisplayName = "Таймаут (мс, 0 = бесконечно)",
                    ValueType = FieldValueType.Constant,
                    AllowedTypeIds = new List<string> { "int" },
                    Required = false,
                    DefaultValue = new ConstantFieldValue(0)
                }
            }
        };
    }
}
