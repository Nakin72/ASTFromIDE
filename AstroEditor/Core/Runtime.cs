using System;
using System.Collections.Generic;
using AstroEditor.Core.Memory;
using AstroEditor.Core.ObjectModel; // Убедитесь, что подключен неймспейс моделей AST

namespace AstroEditor.Core.Runtime
{

    public class ExecutionEngine
    {
        private readonly ProgramContext _context;

        public ExecutionEngine(ProgramContext context)
        {
            _context = context;
        }

        // 1. ЧТЕНИЕ С УЧЕТОМ ОБЛАСТИ ВИДИМОСТИ (ИСПРАВЛЯЕТ CS0103)
        public string GetVariableValue(string varName)
        {
            VariableStorage storage = null;

            // Сначала ищем в локальных переменных программы
            if (_context.LocalVariables.TryGetValue(varName, out var local))
            {
                storage = local;
            }
            // Если не нашли, ищем в глобальной памяти системы (если этот класс у вас реализован)
            // Иначе можно временно закомментировать этот блок
            else if (GlobalSystemMemory.Variables.TryGetValue(varName, out var global))
            {
                storage = global;
            }

            if (storage == null)
                throw new Exception($"Переменная или регистр '{varName}' не найден в локальной или глобальной памяти.");

            // Обработка реактивного оператора связи <=>
            if (storage.IsReference)
            {
                return GetVariableValue(storage.ReferenceTarget); // Рекурсивный обход ссылки
            }

            return storage.RawValue;
        }

        // 2. ЗАПИСЬ С УЧЕТОМ ОБЛАСТИ ВИДИМОСТИ (ИСПРАВЛЯЕТ CS1061)
        public void SetVariableValue(string varName, string newValue)
        {
            VariableStorage storage = null;

            if (_context.LocalVariables.TryGetValue(varName, out var local)) storage = local;
            else if (GlobalSystemMemory.Variables.TryGetValue(varName, out var global)) storage = global;

            if (storage == null)
                throw new Exception($"Некуда писать: переменная '{varName}' не существует.");

            if (storage.IsReference)
            {
                SetVariableValue(storage.ReferenceTarget, newValue); // Пишем напрямую в цель
                return;
            }

            storage.RawValue = newValue;
        }

        // 3. РЕАЛИЗАЦИЯ СВЯЗЫВАНИЯ <=> (ИСПРАВЛЯЕТ CS1061)
        public void BindVariables(string aliasName, string targetName)
        {
            if (_context.LocalVariables.TryGetValue(aliasName, out var local))
            {
                local.IsReference = true;
                local.ReferenceTarget = targetName;
            }
            else if (GlobalSystemMemory.Variables.TryGetValue(aliasName, out var global))
            {
                global.IsReference = true;
                global.ReferenceTarget = targetName;
            }
            else
            {
                throw new Exception($"Переменная-алиас '{aliasName}' должна быть сначала объявлена в программе.");
            }
        }

        // 4. УНИВЕРСАЛЬНЫЙ ВЫЧИСЛИТЕЛЬ ПОЛЯ
        public string EvaluateField(Field field)
        {
            if (field == null || field.DataNodes.Count == 0) return string.Empty;

            if (field.DataNodes.Count == 1)
            {
                return ResolveNodeValue(field.DataNodes[0]);
            }

            string currentResult = string.Empty;
            string currentOp = string.Empty;

            foreach (var node in field.DataNodes)
            {
                if (node is OperatorToken opTok) // ТЕПЕРЬ CS0246 УЙДЕТ, ТАК КАК ПОДКЛЮЧЕН СТРУКТУРНЫЙ НЕЙМСПЕЙС
                {
                    currentOp = opTok.Symbol;
                }
                else
                {
                    string value = ResolveNodeValue(node);

                    if (string.IsNullOrEmpty(currentResult))
                    {
                        currentResult = value;
                    }
                    else if (!string.IsNullOrEmpty(currentOp))
                    {
                        currentResult = EvaluateExpression(currentResult, currentOp, value);
                        currentOp = string.Empty;
                    }
                }
            }

            return currentResult;
        }

        // Вспомогательный метод разрешения узлов
        private string ResolveNodeValue(AstNode node)
        {
            if (node == null) return string.Empty;

            switch (node.NodeType)
            {
                case AstNodeType.Constant:
                    // Явное приведение типов. Компилятор пропустит это без вопросов!
                    var constTok = (ConstantToken)node;
                    return constTok.RawValue;

                case AstNodeType.Object:
                    var objTok = (ObjectToken)node;
                    return GetVariableValue(objTok.Name);

                default:
                    // Для операторов и макросов просто возвращаем их текстовый вид
                    return node.ToTuiString();
            }
        }

        // 5. МАТЕМАТИЧЕСКИЙ И ЛОГИЧЕСКИЙ ДВИЖОК
        public string EvaluateExpression(string leftVal, string op, string rightVal)
        {
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

            throw new Exception($"Несоответствие типов при вычислении: '{leftVal}' {op} '{rightVal}'");
        }
    }

    public class ProgramExecutor
    {
        private readonly ExecutionEngine _engine;
        private readonly ProgramContext _context;
        private int _currentLineIndex = 0;

        // Карта переходов для IF/WHILE/FOR
        private Dictionary<int, int> _jumpTable = new();

        public ProgramExecutor(ProgramContext context, ExecutionEngine engine)
        {
            _context = context;
            _engine = engine;
            AnalyzeStructureAndBuildJumpTable();
        }

        private void AnalyzeStructureAndBuildJumpTable()
        {
            var ifStack = new Stack<int>();
            var whileStack = new Stack<int>();

            for (int i = 0; i < _context.CodeLines.Count; i++)
            {
                var formType = _context.CodeLines[i].CurrentForm.TemplateName;
                if (formType == "IfForm") ifStack.Push(i);
                if (formType == "ElseForm" || formType == "EndIfForm")
                {
                    if (ifStack.TryPop(out int ifLine))
                    {
                        _jumpTable[ifLine] = i;
                    }
                }

                if (formType == "WhileForm") whileStack.Push(i);
                if (formType == "EndWhileForm")
                {
                    if (whileStack.TryPop(out int whileLine))
                    {
                        _jumpTable[i] = whileLine;
                        _jumpTable[whileLine] = i;
                    }
                }
            }
        }

        // Один шаг выполнения программы (Интерпретация одной формы)
        public void Step()
        {
            if (_currentLineIndex >= _context.CodeLines.Count) return;

            var line = _context.CodeLines[_currentLineIndex];
            var form = line.CurrentForm;

            switch (form.TemplateName)
            {
                case "AssignmentForm": // value = 10 + 20 или value = 5
                                       // Извлекаем чистое имя переменной (куда пишем), убирая синтаксическое оформление токена объекта
                    var targetNode = form.Fields[0].DataNodes[0];
                    string varName = (targetNode is ObjectToken obj) ? obj.Name : targetNode.ToTuiString();

                    // Безопасно вычисляем все правое поле целиком (хоть константу, хоть длинную цепочку математики)
                    string res = _engine.EvaluateField(form.Fields[1]);

                    _engine.SetVariableValue(varName, res);
                    _currentLineIndex++;
                    break;

                case "BindForm": // alias <=> target
                    var aliasNode = form.Fields[0].DataNodes[0];
                    var targetNodeRef = form.Fields[1].DataNodes[0];

                    string alias = (aliasNode is ObjectToken o1) ? o1.Name : aliasNode.ToTuiString();
                    string target = (targetNodeRef is ObjectToken o2) ? o2.Name : targetNodeRef.ToTuiString();

                    _engine.BindVariables(alias, target);
                    _currentLineIndex++;
                    break;

                case "IfForm": // IF condition_expression THEN
                               // Безопасно вычисляем все поле условия через универсальный метод
                    bool isTrue = _engine.EvaluateField(form.Fields[0]) == "TRUE";

                    if (isTrue) _currentLineIndex++;
                    else _currentLineIndex = _jumpTable[_currentLineIndex];
                    break;

                case "WhileForm": // WHILE condition_expression
                                  // Вычисляем логическое выражение условия цикла
                    bool isWhileTrue = _engine.EvaluateField(form.Fields[0]) == "TRUE";

                    if (isWhileTrue) _currentLineIndex++;
                    else _currentLineIndex = _jumpTable[_currentLineIndex] + 1;
                    break;

                case "EndWhileForm":
                    _currentLineIndex = _jumpTable[_currentLineIndex];
                    break;

                case "CallForm": // CALL SubProgram
                    var callNode = form.Fields[0].DataNodes[0];
                    string subProgName = (callNode is ObjectToken o3) ? o3.Name : callNode.ToTuiString();

                    ExecuteCall(subProgName);
                    _currentLineIndex++;
                    break;

                default:
                    _currentLineIndex++;
                    break;
            }
        }

        private void ExecuteCall(string subProgramName)
        {
            // Логика оператора CALL (Изолированный контекст подпрограммы)
        }
    }
}