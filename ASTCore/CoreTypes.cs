namespace AstroEditor.Core.Types
{
    // Перечисление для быстрой проверки типа в коде ядра
    public enum BaseTypeCode
    {
        Number,
        String,
        Bool
    }

    // Класс-дескриптор базового типа
    public class RuntimeType
    {
        public BaseTypeCode Code { get; }
        public string Name { get; }
        public string DefaultValue { get; }

        private RuntimeType(BaseTypeCode code, string name, string defaultValue)
        {
            Code = code;
            Name = name;
            DefaultValue = defaultValue;
        }

        // Статические неизменяемые экземпляры базовых типов
        public static readonly RuntimeType Number = new(BaseTypeCode.Number, "Number", "0");
        public static readonly RuntimeType String = new(BaseTypeCode.String, "String", "");
        public static readonly RuntimeType Bool = new(BaseTypeCode.Bool, "Bool", "FALSE");

        // Метод для валидации ввода "на лету"
        public bool IsValueValid(string rawValue)
        {
            return Code switch
            {
                BaseTypeCode.Number => double.TryParse(rawValue, out _),
                BaseTypeCode.Bool => rawValue == "TRUE" || rawValue == "FALSE",
                BaseTypeCode.String => true, // Строка принимает любые символы
                _ => false
            };
        }
    }
}