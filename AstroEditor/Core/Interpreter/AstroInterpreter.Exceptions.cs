// AstroEditor.Core/Interpreter/AstroInterpreter.Exceptions.cs
// Обработчики инструкций для обработки исключений: TRY, CATCH, FINALLY, THROW, RETHROW

using AstroEditor.Core.Programs;
using AstroEditor.Core.Expressions;
using AstroEditor.Core.Variables;

namespace AstroEditor.Core.Interpreter;

public partial class AstroInterpreterEx
{
    #region TRY / CATCH / FINALLY / ENDTRY

    [InstructionHandler("core.try")]
    private void ExecuteTry(Instruction instruction)
    {
        // Создаём новый контекст исключений
        var exceptionContext = new ExceptionContext
        {
            TryStartLineIndex = _state.CurrentLineIndex,
            CatchLineIndex = null,
            FinallyLineIndex = null,
            EndLineIndex = FindMatchingEndTry(_state.CurrentLineIndex),
            ExceptionCaught = false,
            FinallyExecuted = false
        };

        // Ищем позиции CATCH и FINALLY
        FindCatchAndFinally(_state.CurrentLineIndex, out var catchLine, out var finallyLine);
        exceptionContext.CatchLineIndex = catchLine;
        exceptionContext.FinallyLineIndex = finallyLine;

        // Если есть CATCH, ищем переменную для исключения
        if (catchLine.HasValue)
        {
            var catchInstruction = _state.Program.Lines[catchLine.Value];
            if (TryGetFieldValue<VariableFieldValue>(catchInstruction, "exceptionVariable", out var varField))
            {
                exceptionContext.ExceptionVariableName = varField.VariableName;
            }

            // Код ошибки для фильтрации
            if (TryGetFieldValue<ConstantFieldValue>(catchInstruction, "errorCode", out var codeField))
            {
                exceptionContext.ErrorCodeFilter = Convert.ToInt32(codeField.Value ?? 0);
            }
        }

        // Добавляем контекст в стек
        _state.ExceptionStack.Push(exceptionContext);

        // Переходим к следующей строке (начало TRY блока)
        _state.CurrentLineIndex++;
    }

    [InstructionHandler("core.catch")]
    private void ExecuteCatch(Instruction instruction)
    {
        if (_state.ExceptionStack.Count == 0)
            throw new Exception("CATCH without TRY");

        var exceptionContext = _state.ExceptionStack.Peek();

        // Если исключение уже было перехвачено этим блоком, пропускаем
        if (exceptionContext.ExceptionCaught)
        {
            // Переходим к FINALLY или ENDTRY
            if (exceptionContext.FinallyLineIndex.HasValue)
            {
                _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            }
            else
            {
                _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            }
            return;
        }

        // Если нет активного исключения, пропускаем CATCH блок
        if (_state.CurrentException == null)
        {
            // Переходим к FINALLY или ENDTRY
            if (exceptionContext.FinallyLineIndex.HasValue)
            {
                _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            }
            else
            {
                _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            }
            return;
        }

        // Проверяем фильтр по коду ошибки
        if (exceptionContext.ErrorCodeFilter != 0 && 
            _state.CurrentException.ExceptionCode != exceptionContext.ErrorCodeFilter)
        {
            // Код не совпадает, пропускаем этот CATCH
            if (exceptionContext.FinallyLineIndex.HasValue)
            {
                _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            }
            else
            {
                _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            }
            return;
        }

        // Исключение подходит для этого CATCH
        exceptionContext.ExceptionCaught = true;

        // Записываем информацию об исключении в переменную (если указана)
        if (!string.IsNullOrEmpty(exceptionContext.ExceptionVariableName))
        {
            var variable = FindVariable("LocalVariables", exceptionContext.ExceptionVariableName);
            if (variable != null)
            {
                variable.Value = _state.CurrentException.ExceptionMessage ?? $"Error {_state.CurrentException.ExceptionCode}";
            }
        }

        // Продолжаем выполнение с CATCH блока
        _state.CurrentLineIndex++;
    }

    [InstructionHandler("core.finally")]
    private void ExecuteFinally(Instruction instruction)
    {
        if (_state.ExceptionStack.Count == 0)
            throw new Exception("FINALLY without TRY");

        var exceptionContext = _state.ExceptionStack.Peek();

        // Если FINALLY уже выполнялся, пропускаем (защита от повторного выполнения)
        if (exceptionContext.FinallyExecuted)
        {
            _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            return;
        }

        exceptionContext.FinallyExecuted = true;

        // Продолжаем выполнение с FINALLY блока
        _state.CurrentLineIndex++;
    }

    [InstructionHandler("core.endtry")]
    private void ExecuteEndTry(Instruction instruction)
    {
        if (_state.ExceptionStack.Count == 0)
            throw new Exception("ENDTRY without TRY");

        var exceptionContext = _state.ExceptionStack.Pop();

        // Если FINALLY не выполнялся и он есть, выполняем его
        if (!exceptionContext.FinallyExecuted && exceptionContext.FinallyLineIndex.HasValue)
        {
            // Помечаем что FINALLY будет выполнен
            exceptionContext.FinallyExecuted = true;
            _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            return;
        }

        // Очищаем текущее исключение если оно было перехвачено
        if (exceptionContext.ExceptionCaught && _state.CurrentException != null)
        {
            _state.CurrentException = null;
        }

        // Продолжаем после ENDTRY
        _state.CurrentLineIndex = exceptionContext.EndLineIndex;
    }

    #endregion

    #region THROW / RETHROW

    [InstructionHandler("core.throw")]
    private void ExecuteThrow(Instruction instruction)
    {
        var codeField = GetFieldValue<ConstantFieldValue>(instruction, "errorCode");
        var errorCode = Convert.ToInt32(codeField.Value ?? 0);

        var message = "Unknown error";
        if (TryGetFieldValue<ExpressionFieldValue>(instruction, "message", out var msgField))
        {
            // ✅ P1-6: Используем кэш выражений
            var node = ParseCachedExpression(msgField.Expression);
            var ctx = CreateExpressionContext();
            message = _evaluator.Evaluate(node, ctx)?.ToString() ?? $"Error {errorCode}";
        }

        // Создаём исключение
        var exception = new ExceptionContext
        {
            ExceptionCode = errorCode,
            ExceptionMessage = message,
            ExceptionCaught = false
        };

        // Ищем ближайший TRY блок
        if (_state.ExceptionStack.Count > 0)
        {
            // Устанавливаем как текущее исключение
            _state.CurrentException = exception;

            // Выходим из TRY блока и переходим к CATCH
            var exceptionContext = _state.ExceptionStack.Pop();
            
            if (exceptionContext.CatchLineIndex.HasValue)
            {
                // Проверяем фильтр по коду
                if (exceptionContext.ErrorCodeFilter == 0 || 
                    exceptionContext.ErrorCodeFilter == errorCode)
                {
                    // Код подходит или фильтр не установлен
                    exceptionContext.ExceptionCaught = true;
                    
                    // Записываем в переменную если указана
                    if (!string.IsNullOrEmpty(exceptionContext.ExceptionVariableName))
                    {
                        var variable = FindVariable("LocalVariables", exceptionContext.ExceptionVariableName);
                        if (variable != null)
                        {
                            variable.Value = message;
                        }
                    }

                    // Переходим к CATCH
                    _state.CurrentLineIndex = exceptionContext.CatchLineIndex.Value;
                    return;
                }
            }

            // Если CATCH не подошёл, переходим к FINALLY или ENDTRY
            if (exceptionContext.FinallyLineIndex.HasValue)
            {
                _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            }
            else
            {
                _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            }
        }
        else
        {
            // Нет TRY блока — выбрасываем исключение наверх
            _context.RaiseOnError(_state, new Exception(message));
            throw new Exception($"THROW {errorCode}: {message}");
        }
    }

    [InstructionHandler("core.rethrow")]
    private void ExecuteRethrow(Instruction instruction)
    {
        if (_state.CurrentException == null)
            throw new Exception("RETHROW without active exception");

        // Пробрасываем текущее исключение дальше
        if (_state.ExceptionStack.Count > 0)
        {
            var exceptionContext = _state.ExceptionStack.Pop();
            
            if (exceptionContext.FinallyLineIndex.HasValue)
            {
                _state.CurrentLineIndex = exceptionContext.FinallyLineIndex.Value;
            }
            else
            {
                _state.CurrentLineIndex = exceptionContext.EndLineIndex;
            }
        }
        else
        {
            // Нет TRY блока — выбрасываем
            var msg = _state.CurrentException.ExceptionMessage ?? $"Error {_state.CurrentException.ExceptionCode}";
            _context.RaiseOnError(_state, new Exception(msg));
            throw new Exception(msg);
        }
    }

    #endregion

    #region Helpers

    private int FindMatchingEndTry(int startIndex)
    {
        int depth = 0;
        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            if (instr.FormId == "core.try")
                depth++;
            else if (instr.FormId == "core.endtry")
            {
                depth--;
                if (depth == 0)
                    return i + 1; // Возвращаем индекс после ENDTRY
            }
        }
        throw new Exception("No matching ENDTRY found");
    }

    private void FindCatchAndFinally(int startIndex, out int? catchLine, out int? finallyLine)
    {
        catchLine = null;
        finallyLine = null;
        int depth = 0;

        for (int i = startIndex; i < _state.Program.Lines.Count; i++)
        {
            var instr = _state.Program.Lines[i];
            
            if (instr.FormId == "core.try")
                depth++;
            else if (instr.FormId == "core.endtry")
            {
                depth--;
                if (depth == 0)
                    break;
            }
            else if (depth == 1) // Находимся на первом уровне вложенности
            {
                if (instr.FormId == "core.catch" && !catchLine.HasValue)
                {
                    catchLine = i;
                }
                else if (instr.FormId == "core.finally" && !finallyLine.HasValue)
                {
                    finallyLine = i;
                }
            }
        }
    }

    #endregion
}
