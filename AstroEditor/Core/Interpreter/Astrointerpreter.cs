// AstroEditor.Core.v4/Interpreter/AstroInterpreter.cs (полный исправленный код)
using System.Diagnostics;
using AstroEditor.Core.Programs;
using AstroEditor.Core.Tables;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;
using AstroEditor.Core.Alarms;
using AstroEditor.Core.Execution;
using AstroEditor.Core.Common;


namespace AstroEditor.Core.Interpreter;

public class AstroInterpreter
{
    private readonly InterpreterContext _context;
    private InterpreterState _state = null!;
    private readonly Dictionary<string, Action<Instruction>> _instructionHandlers;
    private readonly ExpressionParser _parser = new();
    private readonly ExpressionEvaluator _evaluator = new();
    private bool _isRunning;

public InterpreterState State => _state;
    public InterpreterContext Context => _context;
    public bool IsRunning => _isRunning;

    public AstroInterpreter(InterpreterContext context)
    {
        _context = context;
        _instructionHandlers = new Dictionary<string, Action<Instruction>>
        {
            ["core.assign"] = ExecuteAssign,
            ["core.while"] = ExecuteWhile,
            ["core.endwhile"] = ExecuteEndWhile,
            ["core.lbl"] = ExecuteLabel,
            ["core.jumplbl"] = ExecuteJumpLbl,
            ["core.jumpif"] = ExecuteJumpIf,
            ["core.call"] = ExecuteCall,
            ["core.return"] = ExecuteReturn,
            ["core.break"] = ExecuteBreak,
            ["core.continue"] = ExecuteContinue,
            // Новые управляющие конструкции
            ["core.if"] = ExecuteIf,
            ["core.else"] = ExecuteElse,
            ["core.endif"] = ExecuteEndIf,
            ["core.switch"] = ExecuteSwitch,
            ["core.case"] = ExecuteCase,
            ["core.default"] = ExecuteDefault,
            ["core.endswitch"] = ExecuteEndSwitch,
            ["core.for"] = ExecuteFor,
            ["core.endfor"] = ExecuteEndFor,
            // Аварии
            ["core.alarm.raise"] = ExecuteAlarmRaise,
            ["core.alarm.clear"] = ExecuteAlarmClear,
            ["core.alarm.ack"] = ExecuteAlarmAck,
["core.alarm.clearall"] = ExecuteAlarmClearAll,
// Прерывания
            ["core.interrupt.declare"] = ExecuteInterruptDeclare,
            ["core.interrupt.on"] = ExecuteInterruptOn,
            ["core.interrupt.off"] = ExecuteInterruptOff,
            // Таймеры
            ["core.timer.declare"] = ExecuteTimerDeclare,
            ["core.timer.on"] = ExecuteTimerOn,
            ["core.timer.off"] = ExecuteTimerOff,
            ["core.timer.reset"] = ExecuteTimerReset,
            // WAIT
            ["core.wait"] = ExecuteWait,
        };
    }

    // ========== Публичные методы управления ==========

    public void LoadProgram(AstroProgram program, VariableTableSet? localTables = null)
    {
        _state = new InterpreterState
        {
            Program = program,
            CurrentLocalTables = localTables ?? program.LocalTables,
            CurrentLineIndex = 0,
            CallStack = new Stack<CallFrame>(),
            LoopStack = new Stack<LoopContext>(),
            SwitchStack = new Stack<SwitchContext>(),
            StopRequested = false,
            PauseRequested = false,
            ReturnValue = null
        };

        // Добавляем аргументы в локальные таблицы (если их нет)
        foreach (var arg in program.Arguments)
        {
            var type = _context.TypeRegistry.GetTypeById(arg.TypeId);
            if (type == null)
                throw new Exception($"Тип аргумента '{arg.TypeId}' не найден");

            // Проверяем, есть ли уже переменная с таким именем
            var existing = _state.CurrentLocalTables.FindVariable(arg.Name);
            if (existing == null)
            {
                var variable = new Variable(arg.Name, type, arg.DefaultValue);
                _state.CurrentLocalTables.AddVariable(variable, _context.TypeRegistry);
            }
        }
    }

    public void Run()
    {
        if (_state == null)
            throw new InvalidOperationException("Program not loaded. Call LoadProgram first.");

        _isRunning = true;
        _state.StopRequested = false;
        _state.PauseRequested = false;

        try
        {
            while (!_state.StopRequested && _state.CurrentLineIndex < _state.Program.Lines.Count)
            {
                if (_state.PauseRequested)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var instruction = _state.Program.Lines[_state.CurrentLineIndex];
                _context.RaiseOnBeforeInstruction(_state, instruction);

                try
                {
                    ExecuteInstruction(instruction);
                }
                catch (Exception ex)
                {
                    _context.RaiseOnError(_state, ex);
                    throw new Exception($"Error at line {instruction.LineNumber}: {ex.Message}", ex);
                }

                _context.RaiseOnAfterInstruction(_state, instruction);

                // Если инструкция не изменила текущую строку, переходим к следующей
                if (_state.CurrentLineIndex < _state.Program.Lines.Count &&
                    _state.CurrentLineIndex == _state.Program.Lines.IndexOf(instruction))
                {
                    _state.CurrentLineIndex++;
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Step()
    {
        if (_state == null || _state.StopRequested || _state.CurrentLineIndex >= _state.Program.Lines.Count)
            return;

        var instruction = _state.Program.Lines[_state.CurrentLineIndex];
        _context.RaiseOnBeforeInstruction(_state, instruction);

        try
        {
            ExecuteInstruction(instruction);
        }
        catch (Exception ex)
        {
            _context.RaiseOnError(_state, ex);
            throw;
        }

        _context.RaiseOnAfterInstruction(_state, instruction);
        if (_state.CurrentLineIndex == _state.Program.Lines.IndexOf(instruction))
        {
            _state.CurrentLineIndex++;
        }
    }

    public void Stop() => _state.StopRequested = true;

    public void Pause() => _state.PauseRequested = true;

    public void Resume() => _state.PauseRequested = false;

    public void Reset()
    {
        if (_state != null)
        {
            _state.CurrentLineIndex = 0;
            _state.CallStack.Clear();
            _state.LoopStack.Clear();
            _state.SwitchStack.Clear();
            _state.StopRequested = false;
            _state.PauseRequested = false;
            _state.ReturnValue = null;
        }
    }

    // ========== Внутренние методы ==========

    private void ExecuteInstruction(Instruction instruction)
    {
        if (_instructionHandlers.TryGetValue(instruction.FormId, out var handler))
            handler(instruction);
        else
            throw new NotSupportedException($"Form '{instruction.FormId}' is not supported.");
    }
    #region Обработчики инструкций

    private void ExecuteAssign(Instruction instruction)
    {
        var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
        var targetVar = FindVariable(varField.TableSetName, varField.VariableName);
        if (targetVar == null)
            throw new Exception($"Variable '{varField.VariableName}' not found");

        if (!instruction.Fields.TryGetValue("expression", out var valueField))
            throw new Exception("Field 'expression' not found");

        object? value;
        if (valueField is ExpressionFieldValue exprField)
        {
            var exprNode = _parser.Parse(exprField.Expression);
            var evalContext = CreateExpressionContext();
            value = _evaluator.Evaluate(exprNode, evalContext);
        }
        else if (valueField is ConstantFieldValue constField)
        {
            value = constField.Value;
        }
        else
        {
            throw new Exception($"Unsupported field type for expression: {valueField?.GetType()}");
        }

        targetVar.Value = value;
    }

    private void ExecuteWhile(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var maxIterField = TryGetFieldValue<ConstantFieldValue>(instruction, "maxIterations", out var maxIterVal) ? maxIterVal : null;

        var exprNode = _parser.Parse(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (!Convert.ToBoolean(condition))
        {
            var endIndex = FindMatchingEndWhile(_state.CurrentLineIndex);
            _state.CurrentLineIndex = endIndex;
            return;
        }

        var maxIter = maxIterField != null ? Convert.ToInt32(maxIterField.Value) : 1000;
        var loopContext = new LoopContext
        {
            StartLineIndex = _state.CurrentLineIndex,
            EndLineIndex = FindMatchingEndWhile(_state.CurrentLineIndex),
            MaxIterations = maxIter,
            CurrentIteration = 0
        };
        _state.LoopStack.Push(loopContext);
        _state.CurrentLoopIteration = 0;
    }

    private void ExecuteEndWhile(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("EndWhile without matching While");

        var loop = _state.LoopStack.Peek();
        loop.CurrentIteration++;
        _state.CurrentLoopIteration = loop.CurrentIteration;

        if (loop.CurrentIteration >= loop.MaxIterations)
        {
            _state.LoopStack.Pop();
            _state.CurrentLineIndex = loop.EndLineIndex;
            return;
        }

        _state.CurrentLineIndex = loop.StartLineIndex;
    }

    private void ExecuteLabel(Instruction instruction) { } // ничего не делаем

    private void ExecuteJumpLbl(Instruction instruction)
    {
        var labelField = GetFieldValue<ConstantFieldValue>(instruction, "labelName");
        var labelName = labelField.Value?.ToString();
        if (string.IsNullOrEmpty(labelName))
            throw new Exception("JumpLbl: label name is empty");

        if (_state.Program.Labels.TryGetValue(labelName, out var lineNumber))
        {
            var index = _state.Program.Lines.FindIndex(l => l.LineNumber == lineNumber);
            if (index >= 0)
                _state.CurrentLineIndex = index;
            else
                throw new Exception($"Label '{labelName}' points to line {lineNumber} which does not exist");
        }
        else
        {
            throw new Exception($"Label '{labelName}' not found");
        }
    }
    // private void ExecuteFor(Instruction instruction)
    // {
    //     // Получаем поля
    //     var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
    //     var startField = GetFieldValue<ExpressionFieldValue>(instruction, "start");
    //     var endField = GetFieldValue<ExpressionFieldValue>(instruction, "end");
    //     var stepField = TryGetFieldValue<ExpressionFieldValue>(instruction, "step", out var stepExpr) ? stepExpr : null;

    //     // Находим переменную-счётчик (должна быть числового типа)
    //     var counterVar = FindVariable(varField.TableSetName, varField.VariableName);
    //     if (counterVar == null)
    //         throw new Exception($"Variable '{varField.VariableName}' not found");

    //     // Вычисляем начальное, конечное и шаг
    //     var evalContext = CreateExpressionContext();
    //     var startNode = _parser.Parse(startField.Expression);
    //     var startVal = _evaluator.Evaluate(startNode, evalContext);

    //     var endNode = _parser.Parse(endField.Expression);
    //     var endVal = _evaluator.Evaluate(endNode, evalContext);

    //     object? stepVal = 1;
    //     if (stepField != null)
    //     {
    //         var stepNode = _parser.Parse(stepField.Expression);
    //         stepVal = _evaluator.Evaluate(stepNode, evalContext);
    //     }

    //     // Проверяем, что значения числовые
    //     if (!IsNumeric(startVal) || !IsNumeric(endVal) || !IsNumeric(stepVal))
    //         throw new Exception("FOR loop requires numeric values for start, end, and step");

    //     // Приводим к double для универсальности (можно поддерживать int и double)
    //     var startNum = Convert.ToDouble(startVal);
    //     var endNum = Convert.ToDouble(endVal);
    //     var stepNum = Convert.ToDouble(stepVal);
    //     if (stepNum == 0)
    //         throw new Exception("FOR loop step cannot be zero");

    //     // Определяем направление
    //     bool isIncreasing = stepNum > 0;

    //     // Проверяем условие входа
    //     bool shouldEnter = isIncreasing ? startNum <= endNum : startNum >= endNum;
    //     if (!shouldEnter)
    //     {
    //         // Пропускаем тело цикла до ENDFOR
    //         var endIndex = FindMatchingEndFor(_state.CurrentLineIndex);
    //         _state.CurrentLineIndex = endIndex;
    //         return;
    //     }

    //     // Инициализируем счётчик
    //     counterVar.Value = startVal;

    //     // Создаём контекст цикла
    //     var loopContext = new LoopContext
    //     {
    //         StartLineIndex = _state.CurrentLineIndex,
    //         EndLineIndex = FindMatchingEndFor(_state.CurrentLineIndex),
    //         MaxIterations = 1000, // можно вынести в настройки
    //         CurrentIteration = 0,
    //         IsForLoop = true,
    //         VariableName = varField.VariableName,
    //         StartValue = startVal,
    //         EndValue = endVal,
    //         StepValue = stepVal,
    //         IsIncreasing = isIncreasing,
    //         CurrentValue = startVal
    //     };
    //     _state.LoopStack.Push(loopContext);
    //     _state.CurrentLoopIteration = 0;
    // }

    // private void ExecuteEndFor(Instruction instruction)
    // {
    //     if (_state.LoopStack.Count == 0)
    //         throw new Exception("EndFor without matching For");

    //     var loop = _state.LoopStack.Peek();
    //     if (!loop.IsForLoop)
    //         throw new Exception("EndFor found without For");

    //     loop.CurrentIteration++;
    //     _state.CurrentLoopIteration = loop.CurrentIteration;

    //     // Проверяем лимит итераций
    //     if (loop.CurrentIteration >= loop.MaxIterations)
    //     {
    //         _state.LoopStack.Pop();
    //         _state.CurrentLineIndex = loop.EndLineIndex;
    //         return;
    //     }

    //     // Обновляем переменную-счётчик
    //     var counterVar = FindVariable("", loop.VariableName!); // поиск по всем таблицам
    //     if (counterVar == null)
    //         throw new Exception($"Counter variable '{loop.VariableName}' not found");

    //     // Вычисляем новое значение с шагом
    //     var currentVal = Convert.ToDouble(counterVar.Value);
    //     var stepVal = Convert.ToDouble(loop.StepValue);
    //     var newVal = currentVal + stepVal;
    //     counterVar.Value = newVal;

    //     // Проверяем условие продолжения
    //     bool shouldContinue = loop.IsIncreasing ? newVal <= Convert.ToDouble(loop.EndValue) : newVal >= Convert.ToDouble(loop.EndValue);
    //     if (shouldContinue)
    //     {
    //         // Переходим на начало цикла (FOR)
    //         _state.CurrentLineIndex = loop.StartLineIndex;
    //     }
    //     else
    //     {
    //         // Завершаем цикл
    //         _state.LoopStack.Pop();
    //         _state.CurrentLineIndex = loop.EndLineIndex;
    //     }
    // }
    private void ExecuteFor(Instruction instruction)
    {
        // Проверяем, есть ли уже активный контекст FOR для этой строки (возврат из EndFor)
        var existingLoop = _state.LoopStack.FirstOrDefault(l => l.IsForLoop && l.StartLineIndex == _state.CurrentLineIndex);
        if (existingLoop != null)
        {
            // Повторный вход: проверяем условие продолжения
            var counterVarExisting = FindVariable("", existingLoop.VariableName!);
            if (counterVarExisting == null)
                throw new Exception($"Counter variable '{existingLoop.VariableName}' not found");

            var currentVal = Convert.ToDouble(counterVarExisting.Value);
            var endValExisting = Convert.ToDouble(existingLoop.EndValue);
            var isIncreasingExisting = existingLoop.IsIncreasing;

            bool shouldContinue = isIncreasingExisting ? currentVal <= endValExisting : currentVal >= endValExisting;

            if (shouldContinue)
            {
                // Условие выполнено: продолжаем выполнение тела.
                // Оставляем _state.CurrentLineIndex на текущей строке (FOR).
                // После инкремента в основном цикле перейдём на следующую строку (тело).
            }
            else
            {
                // Условие не выполнено: завершаем цикл
                _state.LoopStack.Pop();
                _state.CurrentLineIndex = existingLoop.EndLineIndex;
            }
            return;
        }

        // Первый вход: создаём контекст
        var varField = GetFieldValue<VariableFieldValue>(instruction, "variable");
        var startField = GetFieldValue<ExpressionFieldValue>(instruction, "start");
        var endField = GetFieldValue<ExpressionFieldValue>(instruction, "end");
        var stepField = TryGetFieldValue<ExpressionFieldValue>(instruction, "step", out var stepExpr) ? stepExpr : null;

        var counterVar = FindVariable(varField.TableSetName, varField.VariableName);
        if (counterVar == null)
            throw new Exception($"Variable '{varField.VariableName}' not found");

        var evalContext = CreateExpressionContext();
        var startVal = _evaluator.Evaluate(_parser.Parse(startField.Expression), evalContext);
        var endVal = _evaluator.Evaluate(_parser.Parse(endField.Expression), evalContext);
        object? stepVal = stepField != null
            ? _evaluator.Evaluate(_parser.Parse(stepField.Expression), evalContext)
            : 1;

        if (!IsNumeric(startVal) || !IsNumeric(endVal) || !IsNumeric(stepVal))
            throw new Exception("FOR loop requires numeric values for start, end, and step");

        var startNum = Convert.ToDouble(startVal);
        var endNum = Convert.ToDouble(endVal);
        var stepNum = Convert.ToDouble(stepVal);
        if (stepNum == 0)
            throw new Exception("FOR loop step cannot be zero");

        bool isIncreasing = stepNum > 0;
        bool shouldEnter = isIncreasing ? startNum <= endNum : startNum >= endNum;
        if (!shouldEnter)
        {
            _state.CurrentLineIndex = FindMatchingEndFor(_state.CurrentLineIndex);
            return;
        }

        counterVar.Value = startVal;

        var loopContext = new LoopContext
        {
            StartLineIndex = _state.CurrentLineIndex,
            EndLineIndex = FindMatchingEndFor(_state.CurrentLineIndex),
            MaxIterations = 1000,
            CurrentIteration = 0,
            IsForLoop = true,
            VariableName = varField.VariableName,
            StartValue = startVal,
            EndValue = endVal,
            StepValue = stepVal,
            IsIncreasing = isIncreasing,
            CurrentValue = startVal
        };
        _state.LoopStack.Push(loopContext);
        // Оставляем _state.CurrentLineIndex на текущей строке FOR
        // После инкремента в основном цикле перейдём на следующую строку (тело)
    }
    private void ExecuteEndFor(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("EndFor without matching For");

        var loop = _state.LoopStack.Peek();
        if (!loop.IsForLoop)
            throw new Exception("EndFor found without For");

        loop.CurrentIteration++;
        if (loop.CurrentIteration >= loop.MaxIterations)
        {
            _state.LoopStack.Pop();
            _state.CurrentLineIndex = loop.EndLineIndex;
            return;
        }

        var counterVar = FindVariable("", loop.VariableName!);
        if (counterVar == null)
            throw new Exception($"Counter variable '{loop.VariableName}' not found");

        var currentVal = Convert.ToDouble(counterVar.Value);
        var stepVal = Convert.ToDouble(loop.StepValue);
        var newVal = currentVal + stepVal;
        counterVar.Value = newVal;

        // Переходим на строку FOR для проверки условия
        _state.CurrentLineIndex = loop.StartLineIndex;
    }

// Вспомогательный метод для проверки числовых типов
    private bool IsNumeric(object? value)
    {
        return value is sbyte || value is byte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }

    // Поиск matching ENDFOR
    private int FindMatchingEndFor(int startIndex) => FindMatchingEnd(startIndex, "core.for", "core.endfor");
    
    private void ExecuteJumpIf(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var labelField = GetFieldValue<ConstantFieldValue>(instruction, "labelName");

        var exprNode = _parser.Parse(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (Convert.ToBoolean(condition))
        {
            var labelName = labelField.Value?.ToString();
            if (string.IsNullOrEmpty(labelName))
                throw new Exception("JumpIf: label name is empty");

            if (_state.Program.Labels.TryGetValue(labelName, out var lineNumber))
            {
                var index = _state.Program.Lines.FindIndex(l => l.LineNumber == lineNumber);
                if (index >= 0)
                    _state.CurrentLineIndex = index;
                else
                    throw new Exception($"Label '{labelName}' points to line {lineNumber} which does not exist");
            }
            else
            {
                throw new Exception($"Label '{labelName}' not found");
            }
        }
    }

    private void ExecuteCall(Instruction instruction)
    {
        var progNameField = GetFieldValue<ConstantFieldValue>(instruction, "programName");
        var progName = progNameField.Value?.ToString();
        if (string.IsNullOrEmpty(progName))
            throw new Exception("Call: program name is empty");

        if (!_context.ProgramRegistry.TryGetValue(progName, out var calledProgram))
            throw new Exception($"Program '{progName}' not found");

        var frame = new CallFrame
        {
            Program = _state.Program,
            ReturnLineIndex = _state.CurrentLineIndex + 1,
            LocalTables = _state.CurrentLocalTables,
            Arguments = new Dictionary<string, object?>()
        };

        if (instruction.Fields.TryGetValue("arguments", out var argsField) && argsField is ConstantFieldValue constArgs)
        {
            if (constArgs.Value is List<object> argsList)
            {
                for (int i = 0; i < Math.Min(argsList.Count, calledProgram.Arguments.Count); i++)
                {
                    var argName = calledProgram.Arguments[i].Name;
                    frame.Arguments[argName] = argsList[i];
                }
            }
        }

        // Клонируем локальные таблицы вызываемой программы
        var newLocalTables = new VariableTableSet
        {
            Name = calledProgram.Name + "_Local",
            IsGlobal = false,
            Tables = new Dictionary<string, TypedVariableTable>()
        };

        foreach (var kv in calledProgram.LocalTables.Tables)
        {
            var newTable = new TypedVariableTable
            {
                TypeId = kv.Key,
                Type = kv.Value.Type,
                Variables = kv.Value.Variables.Select(v => new Variable
                {
                    Name = v.Name,
                    TypeId = v.TypeId,
                    Type = v.Type,
                    Value = v.Value
                }).ToList()
            };
            newLocalTables.Tables[kv.Key] = newTable;
        }

        // Заполняем аргументы
        foreach (var arg in calledProgram.Arguments)
        {
            if (frame.Arguments.TryGetValue(arg.Name, out var argValue))
            {
                var varObj = newLocalTables.FindVariable(arg.Name);
                if (varObj != null)
                    varObj.Value = argValue;
                else
                {
                    var type = _context.TypeRegistry.GetTypeById(arg.TypeId);
                    if (type != null)
                    {
                        var newVar = new Variable(arg.Name, type, argValue);
                        newLocalTables.AddVariable(newVar, _context.TypeRegistry);
                    }
                }
            }
        }

        _state.CallStack.Push(frame);
        _state.Program = calledProgram;
        _state.CurrentLocalTables = newLocalTables;
        _state.CurrentLineIndex = 0;
    }

    private void ExecuteReturn(Instruction instruction)
    {
        if (instruction.Fields.TryGetValue("value", out var valueField))
        {
            if (valueField is ExpressionFieldValue exprField)
            {
                var exprNode = _parser.Parse(exprField.Expression);
                var evalContext = CreateExpressionContext();
                _state.ReturnValue = _evaluator.Evaluate(exprNode, evalContext);
            }
            else if (valueField is ConstantFieldValue constField)
            {
                _state.ReturnValue = constField.Value;
            }
        }

        if (_state.CallStack.Count > 0)
        {
            var frame = _state.CallStack.Pop();
            _state.Program = frame.Program;
            _state.CurrentLocalTables = frame.LocalTables;
            _state.CurrentLineIndex = frame.ReturnLineIndex;
        }
        else
        {
            _state.StopRequested = true;
        }
    }

    private void ExecuteBreak(Instruction instruction)
    {
        // Сначала проверяем Switch
        if (_state.SwitchStack.Count > 0)
        {
            var context = _state.SwitchStack.Pop();
            _state.CurrentLineIndex = context.EndLineIndex;
            return;
        }

        if (_state.LoopStack.Count == 0)
            throw new Exception("Break outside loop or switch");

        var loop = _state.LoopStack.Pop();
        _state.CurrentLineIndex = loop.EndLineIndex;
    }

    private void ExecuteContinue(Instruction instruction)
    {
        if (_state.LoopStack.Count == 0)
            throw new Exception("Continue outside loop");

        var loop = _state.LoopStack.Peek();
        if (loop.IsForLoop)
        {
            // Для FOR переходим на EndFor, чтобы обработать инкремент
            _state.CurrentLineIndex = loop.EndLineIndex - 1; // перейдём на EndFor, затем будет инкремент
        }
        else
        {
            // Для WHILE переходим на начало
            _state.CurrentLineIndex = loop.StartLineIndex;
        }
    }

    // ---- Новые обработчики для IF, ELSE, ENDIF ----

    private void ExecuteIf(Instruction instruction)
    {
        var condField = GetFieldValue<ExpressionFieldValue>(instruction, "condition");
        var exprNode = _parser.Parse(condField.Expression);
        var evalContext = CreateExpressionContext();
        var condition = _evaluator.Evaluate(exprNode, evalContext);

        if (!Convert.ToBoolean(condition))
        {
            var endIndex = FindMatchingEndIf(_state.CurrentLineIndex);
            _state.CurrentLineIndex = endIndex;
        }
    }

    private void ExecuteElse(Instruction instruction)
    {
        // При встрече Else пропускаем до EndIf
        var endIndex = FindMatchingEndIf(_state.CurrentLineIndex);
        _state.CurrentLineIndex = endIndex;
    }

private void ExecuteEndIf(Instruction instruction)
    {
        // Ничего не делаем
    }

    private int FindMatchingEndIf(int startIndex) => FindMatchingEnd(startIndex, "core.if", "core.endif");

    // ---- Обработчики для SWITCH, CASE, DEFAULT, ENDSWITCH ----

    private void ExecuteSwitch(Instruction instruction)
    {
        var exprField = GetFieldValue<ExpressionFieldValue>(instruction, "expression");
        var exprNode = _parser.Parse(exprField.Expression);
        var evalContext = CreateExpressionContext();
        var value = _evaluator.Evaluate(exprNode, evalContext);

        var endIndex = FindMatchingEndSwitch(_state.CurrentLineIndex);
        var context = new SwitchContext
        {
            ExpressionValue = value,
            EndLineIndex = endIndex,
            IsExecuting = false
        };
        _state.SwitchStack.Push(context);

        // Переходим к первому подходящему case или default
        var nextIndex = FindNextCaseOrDefault(_state.CurrentLineIndex + 1, value);
        _state.CurrentLineIndex = nextIndex;
    }

    private void ExecuteCase(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("Case outside switch");

        var context = _state.SwitchStack.Peek();

        // Если уже выполняем ветку, игнорируем остальные case
        if (context.IsExecuting)
            return;

        var valueField = GetFieldValue<ConstantFieldValue>(instruction, "value");
        var caseValue = valueField.Value;

        if (Equals(caseValue, context.ExpressionValue))
        {
            context.IsExecuting = true;
            // Продолжаем выполнение со следующей строки
        }
        else
        {
            // Пропускаем тело этого case до следующего case/default/endswitch
            var nextIndex = FindNextCaseOrDefault(_state.CurrentLineIndex + 1, context.ExpressionValue);
            _state.CurrentLineIndex = nextIndex;
        }
    }

    private void ExecuteDefault(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("Default outside switch");

        var context = _state.SwitchStack.Peek();

        if (context.IsExecuting)
            return; // уже выполняем другую ветку, игнорируем default

        // Выполняем default
        context.IsExecuting = true;
        // Переходим к следующей строке
    }

    private void ExecuteEndSwitch(Instruction instruction)
    {
        if (_state.SwitchStack.Count == 0)
            throw new Exception("EndSwitch without matching Switch");

        var context = _state.SwitchStack.Pop();
        _state.CurrentLineIndex = context.EndLineIndex;
    }

private int FindMatchingEndSwitch(int startIndex) => FindMatchingEnd(startIndex, "core.switch", "core.endswitch");

    private int FindNextCaseOrDefault(int startIndex, object? switchValue)
    {
        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            if (instr.FormId == "core.case")
            {
                var valField = GetFieldValue<ConstantFieldValue>(instr, "value");
                if (Equals(valField.Value, switchValue))
                    return i;
                // иначе пропускаем тело этого case
            }
            else if (instr.FormId == "core.default")
            {
                return i;
            }
            else if (instr.FormId == "core.endswitch")
            {
                return i;
            }
            // иначе игнорируем
        }
        throw new Exception("No matching case/default/endswitch found");
    }

    // ========== Обработчики аварий ==========

    private void ExecuteAlarmRaise(Instruction instruction)
    {
        // Получаем код аварии
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);

        // Получаем AlarmService из контекста
        var alarmSvc = _context.AlarmService;
        if (alarmSvc == null)
            throw new Exception("AlarmService not available");

        // Получаем определение (если не зарегистрирована — создаём временную)
        var def = alarmSvc.GetDefinition(code);
        if (def == null)
        {
            // Пользовательская авария на лету
            var severityStr = "Error";
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "severity", out var sevField))
                severityStr = sevField.Value?.ToString() ?? "Error";

            var severity = severityStr switch
            {
                "Info" => AstroEditor.Core.Common.AlarmSeverity.Info,
                "Warning" => AstroEditor.Core.Common.AlarmSeverity.Warning,
                "Error" => AstroEditor.Core.Common.AlarmSeverity.Error,
                "Fatal" => AstroEditor.Core.Common.AlarmSeverity.Fatal,
                _ => AstroEditor.Core.Common.AlarmSeverity.Error
            };

            def = alarmSvc.CreateUserAlarm($"ALARM_{code}", $"Alarm #{code}", severity);
            def.Fatal = severity == AstroEditor.Core.Common.AlarmSeverity.Fatal;
        }

        // Поднимаем аварию
        var instance = alarmSvc.RaiseFromProgram(
            code,
            _state.Program.Name,
            _state.CurrentLineIndex < _state.Program.Lines.Count
                ? _state.Program.Lines[_state.CurrentLineIndex].LineNumber
                : null
        );

        // Если фатальная — останавливаем программу
        if (def.Fatal)
        {
            _state.StopRequested = true;
            throw new AlarmFatalException(code, def.Name, def.FormatMessage());
        }
    }

    private void ExecuteAlarmClear(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);

        _context.AlarmService?.Clear(code);
    }

    private void ExecuteAlarmAck(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "code");
        var code = Convert.ToInt32(codeField.Value);

        _context.AlarmService?.Acknowledge(code);
    }

private void ExecuteAlarmClearAll(Instruction instruction)
    {
        _context.AlarmService?.ClearAll();
    }

    // ========== Обработчики прерываний ==========

    private void ExecuteInterruptDeclare(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var triggerTypeField = GetFieldValue<ConstantFieldValue>(instruction, "triggerType");
        var handlerField = GetFieldValue<ConstantFieldValue>(instruction, "handlerProgram");

var triggerType = triggerTypeField.Value?.ToString() switch
        {
            "OnChange" => InterruptTrigger.OnChange,
            "OnRisingEdge" => InterruptTrigger.OnRisingEdge,
            "OnFallingEdge" => InterruptTrigger.OnFallingEdge,
            "OnValue" => InterruptTrigger.OnValue,
            "OnTimer" => InterruptTrigger.OnTimer,
            "OnAlarm" => InterruptTrigger.OnAlarm,
            _ => InterruptTrigger.OnChange
        };

        var execModeStr = "Deferred";
        if (TryGetFieldValue<ConstantFieldValue>(instruction, "executionMode", out var execField))
            execModeStr = execField.Value?.ToString() ?? "Deferred";

var execMode = execModeStr switch
        {
            "Deferred" => AstroEditor.Core.Common.InterruptExecutionMode.Deferred,
            "Background" => AstroEditor.Core.Common.InterruptExecutionMode.Background,
            "Inline" => AstroEditor.Core.Common.InterruptExecutionMode.Inline,
            _ => AstroEditor.Core.Common.InterruptExecutionMode.Deferred
        };

var def = new InterruptDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = nameField.Value?.ToString() ?? "Unnamed",
            TriggerType = triggerType,
            Expression = TryGetFieldValue<ExpressionFieldValue>(instruction, "expression", out var exprF)
                ? exprF.Expression : null,
            VariableName = TryGetFieldValue<VariableFieldValue>(instruction, "variableName", out var varF)
                ? varF.VariableName : null,
            AlarmCode = TryGetFieldValue<ConstantFieldValue>(instruction, "alarmCode", out var alarmF)
                ? Convert.ToInt32(alarmF.Value) : null,
            TimerIntervalMs = TryGetFieldValue<ConstantFieldValue>(instruction, "timerMs", out var timerF)
                ? Convert.ToInt32(timerF.Value) : null,
            HandlerProgramName = handlerField.Value?.ToString() ?? string.Empty,
            ExecutionMode = execMode,
            IsEnabled = true
        };

        _context.InterruptService?.Register(def);
    }

    private void ExecuteInterruptOn(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var def = _context.InterruptService?.GetDefinitionByName(name);
            if (def != null)
                _context.InterruptService?.Enable(def.Id);
        }
    }

private void ExecuteInterruptOff(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var def = _context.InterruptService?.GetDefinitionByName(name);
            if (def != null)
                _context.InterruptService?.Disable(def.Id);
        }
    }

    // ========== Обработчики таймеров ==========

    private void ExecuteTimerDeclare(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var intervalField = GetFieldValue<ConstantFieldValue>(instruction, "intervalMs");

        var modeStr = "Periodic";
        if (TryGetFieldValue<ConstantFieldValue>(instruction, "mode", out var modeField))
            modeStr = modeField.Value?.ToString() ?? "Periodic";

var mode = modeStr == "Oneshot"
            ? TimerMode.Oneshot
            : TimerMode.Periodic;

        var timerDef = new TimerDefinition
        {
            Name = nameField.Value?.ToString() ?? "UnnamedTimer",
            IntervalMs = Convert.ToInt32(intervalField.Value),
            Mode = mode,
            HandlerProgramName = TryGetFieldValue<ConstantFieldValue>(instruction, "handlerProgram", out var hf)
                ? hf.Value?.ToString() : null
        };

        _context.TimerService?.Register(timerDef);
    }

    private void ExecuteTimerOn(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Enable(timer.Id);
        }
    }

    private void ExecuteTimerOff(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Disable(timer.Id);
        }
    }

    private void ExecuteTimerReset(Instruction instruction)
    {
        var nameField = GetFieldValue<ConstantFieldValue>(instruction, "name");
        var name = nameField.Value?.ToString();

        if (name != null)
        {
            var timer = _context.TimerService?.GetTimer(name);
            if (timer != null)
                _context.TimerService?.Reset(timer.Id);
        }
    }

    // ========== Обработчик WAIT ==========

    private void ExecuteWait(Instruction instruction)
    {
        var modeField = GetFieldValue<ConstantFieldValue>(instruction, "mode");
        var mode = modeField.Value?.ToString() ?? "Time";

        if (mode == "Time")
        {
            var timeMs = 1000;
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "timeMs", out var timeField))
                timeMs = Convert.ToInt32(timeField.Value);

            // Приостанавливаем выполнение через Thread.Sleep в фоне
            // В реальном планировщике — переключаем задачу в ожидание
            Thread.Sleep(timeMs);
        }
        else if (mode == "Condition")
        {
            var timeoutMs = 0;
            if (TryGetFieldValue<ConstantFieldValue>(instruction, "timeoutMs", out var toutField))
                timeoutMs = Convert.ToInt32(toutField.Value);

            var conditionExpr = TryGetFieldValue<ExpressionFieldValue>(instruction, "condition", out var condField)
                ? condField.Expression : null;

            if (string.IsNullOrEmpty(conditionExpr))
                return; // нет условия — просто выходим

            var stopwatch = Stopwatch.StartNew();
            var parser = new ExpressionParser();
            var evaluator = new ExpressionEvaluator();

            while (true)
            {
                if (_state.StopRequested)
                    return;

                // Проверяем условие
                try
                {
                    var ctx = CreateExpressionContext();
                    var node = parser.Parse(conditionExpr);
                    var result = evaluator.Evaluate(node, ctx);

                    if (Convert.ToBoolean(result))
                        return; // условие выполнено
                }
                catch { /* игнорируем ошибки парсинга */ }

                // Проверка таймаута
                if (timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs)
                    return;

                Thread.Sleep(50); // опрос 20 Гц
            }
        }
    }

    #endregion

    #region Вспомогательные методы

    private T GetFieldValue<T>(Instruction instruction, string fieldName) where T : FieldValue
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && field is T typed)
            return typed;
        throw new Exception($"Field '{fieldName}' not found or not of type {typeof(T).Name}");
    }

    private bool TryGetFieldValue<T>(Instruction instruction, string fieldName, out T? value) where T : FieldValue
    {
        if (instruction.Fields.TryGetValue(fieldName, out var field) && field is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    private Variable? FindVariable(string tableSetName, string variableName)
    {
        if (tableSetName == "Global")
            return _context.GlobalTables.FindVariable(variableName);
        else if (tableSetName == "LocalVariables" || tableSetName == _state.Program.Name + "_Local")
            return _state.CurrentLocalTables.FindVariable(variableName);
        else
        {
            var found = _state.CurrentLocalTables.FindVariable(variableName);
            if (found != null) return found;
            return _context.GlobalTables.FindVariable(variableName);
        }
    }

    private ExpressionContext CreateExpressionContext()
    {
        return new ExpressionContext
        {
            GlobalTables = _context.GlobalTables,
            LocalTables = _state.CurrentLocalTables,
            TypeRegistry = _context.TypeRegistry,
            Functions = _context.Functions
        };
    }

private int FindMatchingEndWhile(int startIndex) => FindMatchingEnd(startIndex, "core.while", "core.endwhile");
    
    private int FindMatchingEnd(int startIndex, string openFormId, string closeFormId)
    {
        int depth = 0;
        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            if (instr.FormId == openFormId)
                depth++;
            else if (instr.FormId == closeFormId)
            {
                depth--;
                if (depth == 0)
                    return i + 1;
            }
        }
        throw new Exception($"No matching '{closeFormId}' found for '{openFormId}'");
    }

    #endregion
}