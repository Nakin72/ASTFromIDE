namespace AstroEditor.Core.Memory
{
    public class VariableStorage
    {
        public string Name { get; set; }
        public string TypeName { get; set; } // "Number", "Bool", "String" или имя кастомной структуры

        // Значение всегда храним в строковом виде для универсальности и простоты JSON-сериализации
        public string RawValue { get; set; }

        // Поля для поддержки массивов/списков (опционально)
        public bool IsArray { get; set; } = false;
        public int ArraySize { get; set; } = 0;

        // Поля для поддержки промышленных фич (Алиасы / Оператор связывания <=>)
        public bool IsReference { get; set; } = false;
        public string ReferenceTarget { get; set; } // Имя переменной или ID железного порта, на который мы ссылаемся

        // Комментарий (Лейбл), привязанный к переменной/регистру
        public string Comment { get; set; } = string.Empty;
    }
}