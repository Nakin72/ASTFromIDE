using System;
using AstroEditor.Core.Memory;

namespace AstroEditor.Core.Runtime
{
    public class ExecutionEngine
    {
        private readonly ProgramContext _context;

        public ExecutionEngine(ProgramContext context)
        {
            _context = context;
        }

        // 1. ПОЛУЧЕНИЕ ЗНАЧЕНИЯ (С учетом реактивных связей <=>)
        public string GetVariableValue(string varName)
        {
            if (!_context.Variables.TryGetValue(varName, out var storage))
                throw new Exception($"Переменная {varName} не найдена в памяти программы.");

            // Если это ссылка (<=>), рекурсивно идем по адресу цели (железо или другая переменная)
            if (storage.IsReference)
            {
                return GetVariableValue(storage.ReferenceTarget);
            }

            return storage.RawValue;
        }

        // 2. ЗАПИСЬ ЗНАЧЕНИЯ (С учетом реактивных связей <=>)
        public void SetVariableValue(string varName, string newValue)
        {
            if (!_context.Variables.TryGetValue(varName, out var storage))
                throw new Exception($"Переменная {varName} не найдена.");

            // Если это ссылка (<=>), пишем напрямую в физический целевой объект
            if (storage.IsReference)
            {
                SetVariableValue(storage.ReferenceTarget, newValue);
                return;
            }

            storage.RawValue = newValue;
        }

        // 3. РЕАЛИЗАЦИЯ СВЯЗЫВАНИЯ (<=>)
        public void BindVariables(string aliasName, string targetName)
        {
            if (!_context.Variables.TryGetValue(aliasName, out var aliasStorage))
                throw new Exception($"Локальная переменная {aliasName} не объявлена.");

            aliasStorage.IsReference = true;
            aliasStorage.ReferenceTarget = targetName; // Теперь aliasName смотрит на targetName
        }

        // 4. ВЫЧИСЛЕНИЕ МАТЕМАТИКИ И ЛОГИКИ
        public string EvaluateExpression(string left, string op, string right)
        {
            // Пытаемся разрешить имена переменных в значения, если это не константы
            string leftVal = _context.Variables.ContainsKey(left) ? GetVariableValue(left) : left;
            string rightVal = _context.Variables.ContainsKey(right) ? GetVariableValue(right) : right;

            if (double.TryParse(leftVal, out double lNum) && double.TryParse(rightVal, out double rNum))
            {
                return op switch
                {
                    "+" => (lNum + rNum).ToString(),
                    "-" => (lNum - rNum).ToString(),
                    "*" => (lNum * rNum).ToString(),
                    "/" => (lNum / rNum).ToString(),
                    ">" => (lNum > rNum).ToString().ToUpper(),
                    "<" => (lNum < rNum).ToString().ToUpper(),
                    "==" => (lNum == rNum).ToString().ToUpper(),
                    _ => throw new NotSupportedException($"Оператор {op} не поддерживается для чисел.")
                };
            }

            if (bool.TryParse(leftVal, out bool lBool) && bool.TryParse(rightVal, out bool rBool))
            {
                return op switch
                {
                    "AND" => (lBool && rBool).ToString().ToUpper(),
                    "OR" => (lBool || rBool).ToString().ToUpper(),
                    "==" => (lBool == rBool).ToString().ToUpper(),
                    _ => throw new NotSupportedException($"Оператор {op} не поддерживается для Bool.")
                };
            }

            throw new Exception("Несоответствие типов данных в выражении.");
        }
    }
}