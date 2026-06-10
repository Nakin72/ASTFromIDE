using AstroEditor.Core.Types;
namespace AstroEditor.Core.ObjectModel
{ // Убедитесь, что подключен неймспейс моделей AST

    // 0. ПЕРЕЧИСЛЕНИЕ ДЛЯ БЫСТРОГО ОПРЕДЕЛЕНИЯ ТИПА УЗЛА ЯДРОМ
    public enum AstNodeType
    {
        Constant,
        Object,
        Functional,
        Operator,
        MacroCall,
        Slot
    }

    // БАЗОВЫЙ АБСТРАКТНЫЙ КЛАСС (Вместо интерфейса I_AstNode)
    public abstract class AstNode
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        // Каждому токену придется сказать ядру, какой он группы
        public abstract AstNodeType NodeType { get; }

        public abstract string ToTuiString();
    }

    // 1. УНИВЕРСАЛЬНЫЙ ФУНКЦИОНАЛЬНЫЙ ТОКЕН (Переменная / Массив / Константа)
    public class FunctionalToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.Functional;
        public BaseType? ReturnType { get; set; }

        // Поля данных
        public string Name { get; set; } = string.Empty;          // "foo", "flag", "125"

        // Ссылка на параметр может быть null, если это не массив (ставим значок '?')
        public AstNode? Parameter { get; set; }                     // То, что в []
        public string Comment { get; set; } = string.Empty;       // Комментарий/Лейбл
        public string Value { get; set; } = string.Empty;         // Значение

        public override string ToTuiString()
        {
            string result = Name;
            if (Parameter != null) result += $"[{Parameter.ToTuiString()}]";
            if (!string.IsNullOrEmpty(Comment)) result += $":({Comment})";
            if (!string.IsNullOrEmpty(Value)) result += $": {Value}";
            return result;
        }
    }

    // 2. ТОКЕН ОПЕРАТОРА (+, -, *, <=>)
    public class OperatorToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.Operator;

        public string Symbol { get; set; } = string.Empty;  // "+", "=>"
        public BaseType ReturnType => BaseType.Void;

        public override string ToTuiString() => $" {Symbol} ";
    }

    // 3. ТОКЕН ВОЗВРАЩАЮЩЕГО МАКРОСА / ФУНКЦИИ (sin(...), ReadSensor())
    public class MacroCallToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.MacroCall;

        public string MacroName { get; set; } = string.Empty; // "sin", "ReadLaser"
        public BaseType? ReturnType { get; set; }

        // Теперь список хранит жесткие AstNode. Ошибки приведения типов больше не будет!
        public List<AstNode> Arguments { get; set; } = new();

        public override string ToTuiString()
        {
            var argsStr = string.Join(", ", Arguments.Select(a => a.ToTuiString()));
            return $"{MacroName}({argsStr})";
        }
    }

    // 4. ПУСТОЙ ТОКЕН-СЛОТ (Placeholder)
    public class SlotToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.Slot;

        public BaseType? ExpectedType { get; set; }
        public string Label { get; set; } = "???";

        public BaseType? ReturnType => ExpectedType;
        public override string ToTuiString() => $"[{Label}]";
    }

    // ТОКЕН КОНСТАНТЫ
    public class ConstantToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.Constant;

        public string RawValue { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;

        public override string ToTuiString() => RawValue;
    }
    // ==========================================
    // УРОВЕНЬ 3: ПОЛЯ (КОНТЕЙНЕРЫ ДЛЯ ДАННЫХ)
    // ==========================================
    public class Field
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }          // Имя слота ("Condition", "LeftValue")
        public required string ExpectedDataType { get; set; }  // Ограничение типа ("Number", "Bool")

        // В поле заносятся данные: функциональные токены или константы
        public List<AstNode> DataNodes { get; set; } = new();

        // Оформление самого поля (например, пробелы или запятые между токенами внутри поля)
        public string Separator { get; set; } = " ";

        public string ToTuiString()
        {
            return string.Join(Separator, DataNodes.ConvertAll(n => n.ToTuiString()));
        }
    }
    // ==========================================
    // УРОВЕНЬ 2: ФОРМЫ (КАРКАС СТРОКИ)
    // ==========================================
    public class Form
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public required string TemplateName { get; set; }   // "AssignmentForm", "IfThenForm"

        // Форма состоит из полей
        public List<Field> Fields { get; set; } = new();

        // Текстовые разделители каркаса формы (например, ["IF ", " THEN "])
        public List<string> StructuralSeparators { get; set; } = new();
    }
   

    

    // ==========================================
    // УРОВЕНЬ 1: СТРОКИ И ПРОГРАММА
    // ==========================================
    public class Line
    {
        public int LineNumber { get; set; }
        public int IndentLevel { get; set; } = 0;

        // Строка состоит из формы
        public required Form CurrentForm { get; set; }
    }
    // ТОКЕН ОБЪЕКТА
    public class ObjectToken : AstNode
    {
        public override AstNodeType NodeType => AstNodeType.Object;

        public string Name { get; set; } = string.Empty;
        public AstNode? ArrayIndex { get; set; }
        public string Comment { get; set; } = string.Empty;
        public AstNode? Value { get; set; }

        public override string ToTuiString()
        {
            string res = Name;
            if (ArrayIndex != null) res += $"[{ArrayIndex.ToTuiString()}]";
            if (Value != null) res += $": {Value.ToTuiString()}";
            if (!string.IsNullOrEmpty(Comment)) res += $" :({Comment})";
            return res;
        }
    }

    public class ProgramProject
    {
        public required string Name { get; set; }

        // Программа состоит из строк
        public List<Line> Lines { get; set; } = new();
    }
}