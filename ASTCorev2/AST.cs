using System;
using System.Collections.Generic;
using AstroEditor.Core.SystemTypes;
using System.Text.Json.Serialization; // Добавить сюда

namespace AstroEditor.Core.AST
{
    // Интерфейс для всего, что может быть вычислено и возвращает SystemContainer
    [JsonDerivedType(typeof(LiteralField), typeDiscriminator: "Literal")]
    [JsonDerivedType(typeof(VariableField), typeDiscriminator: "Variable")]
    [JsonDerivedType(typeof(ComplexExpression), typeDiscriminator: "Complex")]
    public interface I_Expression
    {
        public string ExpectedType { get; }
        public SystemContainer Evaluate(VariableTable localTable, VariableTable globalTable);
    }
    public class LiteralField : I_Expression
    {
        public string ExpectedType { get; init; }

        // Само значение (например, 343434 или true)
        public object Value { get; set; }

        public LiteralField(string expectedType, object value)
        {
            ExpectedType = expectedType;
            Value = value;
        }

        public SystemContainer Evaluate(VariableTable localTable, VariableTable globalTable)
        {
            // Фабрика или switch-expression создает нужный SystemContainer на лету
            return ExpectedType switch
            {
                "Number" => new SystemNumberContainer(Value, string.Empty),
                "Boolean" => new SystemBoolContainer(Value, string.Empty),
                "String" => new SystemStringContainer(Value, string.Empty),
                _ => new SystemNumberContainer(Value, string.Empty) // Или ваша логика для кастомных структур
            };
        }
    }
    public class VariableField : I_Expression
    {
        public string ExpectedType { get; init; }
        public string VariableName { get; set; }
        public bool IsGlobal { get; set; }

        public VariableField(string expectedType, string variableName, bool isGlobal = false)
        {
            ExpectedType = expectedType;
            VariableName = variableName;
            IsGlobal = isGlobal;
        }

        public SystemContainer Evaluate(VariableTable localTable, VariableTable globalTable)
        {
            var table = IsGlobal ? globalTable : localTable;

            if (table.TryGet(VariableName, out var container))
            {
                // Строгая проверка типов на соответствие ожидаемому типу формы
                if (container.Type != ExpectedType && ExpectedType != "Any")
                {
                    throw new InvalidOperationException($"Тип переменной {container.Type} не совпадает с ожидаемым {ExpectedType}");
                }
                return container;
            }

            throw new KeyNotFoundException($"Переменная {VariableName} не найдена.");
        }

        // Тот самый метод для UI, который вы просили (foo:343434 или flag:off)
        public string GetDisplayString(VariableTable localTable, VariableTable globalTable)
        {
            try
            {
                var container = Evaluate(localTable, globalTable);

                // Обрабатываем отображение на основе CoreTypeNum
                // CoreTypeNum зашит в ваших классах (1 = Number, 5 = Boolean)
                return container.CoreType switch
                {
                    "CoreNumber" => $"{VariableName}:{container.Data}",
                    "CoreBoolean" => $"{VariableName}:{((bool)container.Data ? "on" : "off")}",
                    _ => $"{VariableName}:{container.Type}" // Для строк, списков и структур пишем тип
                };
            }
            catch
            {
                return $"{VariableName}:ERR"; // Если переменная еще не создана
            }
        }
    }
    public class ComplexExpression : I_Expression
    {
        public string ExpectedType { get; init; }

        // Идентификатор операции (например, "ADD", "COMPARE", "GET_STRUCT_FIELD")
        public string OperationId { get; init; }

        // Вложенные выражения. Сюда можно передать LiteralField, VariableField или другой ComplexExpression!
        public Dictionary<string, I_Expression> Slots { get; } = new();

        public ComplexExpression(string expectedType, string operationId)
        {
            ExpectedType = expectedType;
            OperationId = operationId;
        }

        public SystemContainer Evaluate(VariableTable localTable, VariableTable globalTable)
        {
            // Логика зависит от OperationId
            if (OperationId == "ADD")
            {
                var left = Slots["Left"].Evaluate(localTable, globalTable);
                var right = Slots["Right"].Evaluate(localTable, globalTable);

                // Так как у вас динамика работает, складываем через dynamic
                dynamic v1 = left.Data;
                dynamic v2 = right.Data;
                return new SystemNumberContainer(v1 + v2, string.Empty, ExpectedType);
            }

            throw new NotImplementedException($"Операция {OperationId} не реализована.");
        }
    }
    public class ProgramLine
    {
        public int LineNumber { get; set; }

        // Корневое выражение этой строки (например, форма присвоения "=")
        public ComplexExpression RootFormExpression { get; set; }

        public void Execute(VariableTable localTable, VariableTable globalTable)
        {
            // Выполнение строки — это просто вычисление её корневой формы
            RootFormExpression.Evaluate(localTable, globalTable);
        }
    }
    public class VariableTable
    {
        // Внутреннее хранилище: Имя переменной -> Контейнер с данными
        private readonly Dictionary<string, SystemContainer> _storage = new(StringComparer.OrdinalIgnoreCase);

        // Событие для UI: вызывается, когда добавлена переменная или изменилось её значение
        public event Action<string, SystemContainer>? OnTableChanged;

        /// <summary>
        /// Регистрация или обновление переменной в таблице.
        /// </summary>
        public void Set(string name, SystemContainer container)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя переменной не может быть пустым.", nameof(name));

            _storage[name] = container;

            // Триггерим событие, чтобы UI перерисовал значения "в прямом эфире"
            OnTableChanged?.Invoke(name, container);
        }

        /// <summary>
        /// Поиск переменной по имени.
        /// </summary>
        public bool TryGet(string name, out SystemContainer container)
        {
            return _storage.TryGetValue(name, out container!);
        }

        /// <summary>
        /// Удаление переменной из таблицы (например, при удалении из меню локальных/глобальных переменных).
        /// </summary>
        public bool Remove(string name)
        {
            if (_storage.Remove(name, out var container))
            {
                OnTableChanged?.Invoke(name, container);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Метод для Blockly-интерфейса: возвращает список имен переменных, 
        /// которые подходят под ожидаемый тип поля формы.
        /// </summary>
        /// <param name="expectedType">Зарегистрированный системный или кастомный тип (например, "Number")</param>
        public IEnumerable<string> GetAvailableVariablesForType(string expectedType)
        {
            // Если поле принимает "Any", отдаем абсолютно все переменные
            if (expectedType == "Any")
            {
                return _storage.Keys;
            }

            // Иначе фильтруем строго по совпадению SystemContainer.Type с зарегистрированным типом
            return _storage
                .Where(pair => pair.Value.Type == expectedType)
                .Select(pair => pair.Key);
        }

        /// <summary>
        /// Позволяет UI напрямую обновить .Data внутри контейнера по имени переменной
        /// </summary>
        public void UpdateValueDirectly(string name, object newValue)
        {
            if (_storage.TryGetValue(name, out var container))
            {
                container.Data = newValue;
                OnTableChanged?.Invoke(name, container);
            }
        }
    }
public class AstroProgram
    {
        // Имя программы (например, "MAIN_LOGIC", "PICK_PART")
        public string Name { get; init; }

        // Список строк программы. Каждая строка — это форма-выражение (Fanuc TP style)
        public List<ProgramLine> Lines { get; } = new();

        // Локальная таблица переменных, доступная только внутри этой программы
        public VariableTable LocalVariables { get; } = new();

        // Описание входных параметров (аргументов), которые программа ожидает при вызове (CALL)
        // Ключ — имя аргумента, Значение — ожидаемый тип данных из реестра
        public Dictionary<string, string> ArgumentDefinitions { get; } = new(StringComparer.OrdinalIgnoreCase);

        public AstroProgram(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя программы не может быть пустым.", nameof(name));
            Name = name;
        }

        /// <summary>
        /// Добавление описания входного аргумента в спец. меню
        /// </summary>
        public void AddArgumentDefinition(string argName, string expectedType)
        {
            if (!SystemContainer.CustomTypes.Contains(expectedType) && expectedType != "Any")
            {
                throw new InvalidOperationException($"Тип данных '{expectedType}' не зарегистрирован в системе.");
            }
            ArgumentDefinitions[argName] = expectedType;
        }

        /// <summary>
        /// Запуск выполнения программы (Интерпретатор AST)
        /// </summary>
        /// <param name="globalVariables">Ссылка на общую таблицу глобальных переменных</param>
        /// <param name="passedArguments">Список контейнеров-значений, переданных при вызове программы</param>
        public void Execute(VariableTable globalVariables, List<SystemContainer>? passedArguments = null)
        {
            // 1. Валидация и инициализация аргументов во внутренней таблице
            InitializeArguments(passedArguments);

            // 2. Последовательное выполнение строк программы (Fanuc TP Style)
            // Используем обычный for, чтобы в будущем было легко сделать переходы JUMP / LBL (метки)
            for (int i = 0; i < Lines.Count; i++)
            {
                try
                {
                    // Выполняем форму на текущей строке
                    Lines[i].Execute(LocalVariables, globalVariables);
                }
                catch (Exception ex)
                {
                    // Перехват ошибки выполнения с указанием конкретной строки для отладки в UI
                    throw new RuntimeException($"Ошибка на строке {Lines[i].LineNumber} ({ex.Message})", ex);
                }
            }
        }

        private void InitializeArguments(List<SystemContainer>? passedArguments)
        {
            passedArguments ??= new List<SystemContainer>();

            // Проверяем количество переданных аргументов
            if (passedArguments.Count != ArgumentDefinitions.Count)
            {
                throw new ArgumentException($"Программа '{Name}' ожидает {ArgumentDefinitions.Count} аргументов, но передано {passedArguments.Count}.");
            }

            int index = 0;
            foreach (var def in ArgumentDefinitions)
            {
                var passedArg = passedArguments[index];

                // Проверяем тип переданного аргумента на соответствие сигнатуре
                if (def.Value != "Any" && passedArg.Type != def.Value)
                {
                    throw new InvalidCastException($"Аргумент '{def.Key}' ожидает тип '{def.Value}', но передан '{passedArg.Type}'.");
                }

                // Клонируем или проксируем аргумент в локальную таблицу
                // Так как это передача аргумента, создаем локальную копию с оригинальным именем параметра
                LocalVariables.Set(def.Key, passedArg); 
                index++;
            }
        }
    }

    // Кастомное исключение рантайма для удобного вывода ошибок в лог редактора
    public class RuntimeException : Exception
    {
        public RuntimeException(string message, Exception innerException) : base(message, innerException) { }
    }
}