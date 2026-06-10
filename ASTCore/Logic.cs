
using System;
using AstroEditor.Core.Memory;
namespace AstroEditor.Core.Runtime
{
    public class ProgramExecutor
    {
        
        private readonly ExecutionEngine _engine;
        private readonly ProgramContext _context;
        private int _currentLineIndex = 0;

        // Карта переходов для IF/WHILE/FOR (какая строка куда перепрыгивает при невыполнении условия)
        private Dictionary<int, int> _jumpTable = new();

        public ProgramExecutor(ProgramContext context, ExecutionEngine engine)
        {
            _context = context;
            _engine = engine;
            AnalyzeStructureAndBuildJumpTable();
        }

        // Анализируем структуру строк (IndentLevel, шаблоны форм) и связываем блоки намертво
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
                        _jumpTable[ifLine] = i; // Если IF ложен, прыгаем на ELSE или ENDIF
                    }
                }

                if (formType == "WhileForm") whileStack.Push(i);
                if (formType == "EndWhileForm")
                {
                    if (whileStack.TryPop(out int whileLine))
                    {
                        _jumpTable[i] = whileLine;   // ENDWHILE прыгает обратно на начало проверки
                        _jumpTable[whileLine] = i;  // WHILE при ложном условии прыгает за ENDWHILE
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
                case "AssignmentForm": // value = 10 + 20
                    string varName = form.Fields[0].DataNodes[0].ToTuiString(); // Левое поле
                    // Для простоты примера берем плоское выражение из правого поля
                    string leftOp = form.Fields[1].DataNodes[0].ToTuiString();
                    string sign = form.Fields[1].DataNodes[1].ToTuiString();
                    string rightOp = form.Fields[1].DataNodes[2].ToTuiString();

                    string res = _engine.EvaluateExpression(leftOp, sign, rightOp);
                    _engine.SetVariableValue(varName, res);
                    _currentLineIndex++;
                    break;

                case "BindForm": // alias <=> target
                    string alias = form.Fields[0].DataNodes[0].ToTuiString();
                    string target = form.Fields[1].DataNodes[0].ToTuiString();
                    _engine.BindVariables(alias, target);
                    _currentLineIndex++;
                    break;

                case "IfForm": // IF condition
                    string conditionVar = form.Fields[0].DataNodes[0].ToTuiString();
                    bool isTrue = _engine.GetVariableValue(conditionVar) == "TRUE";

                    if (isTrue) _currentLineIndex++; // Просто идем на следующую строчку внутрь IF
                    else _currentLineIndex = _jumpTable[_currentLineIndex]; // Прыгаем на ELSE/ENDIF
                    break;

                case "WhileForm": // WHILE condition
                    string whileCond = form.Fields[0].DataNodes[0].ToTuiString();
                    bool isWhileTrue = _engine.GetVariableValue(whileCond) == "TRUE";

                    if (isWhileTrue) _currentLineIndex++;
                    else _currentLineIndex = _jumpTable[_currentLineIndex] + 1; // Выходим из цикла
                    break;

                case "EndWhileForm": // Замыкание цикла прыгает наверх
                    _currentLineIndex = _jumpTable[_currentLineIndex];
                    break;

                case "CallForm": // CALL SubProgram
                    string subProgName = form.Fields[0].DataNodes[0].ToTuiString();
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
            // Логика оператора CALL:
            // 1. Находим файл/ресурс subProgramName через StorageManager.LoadSession
            // 2. Создаем для него отдельный ProgramExecutor
            // 3. Выполняем его до конца (или изолированно шагаем по нему)
            // Пром. особенность: подпрограмма имеет СВОЙ изолированный ProgramContext и свои локальные переменные!
        }
    }
}