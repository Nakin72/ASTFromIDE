using System.Collections.Generic;

using AstroEditor.Core.ObjectModel;
namespace AstroEditor.Core.Memory
{
    // Глобальная память ВСЕЙ системы (сквозные регистры/переменные)
    public static class GlobalSystemMemory
    {
        // Сюда пишутся переменные с модификатором GLOBAL, системные I/O и регистры
        public static Dictionary<string, VariableStorage> Variables { get; set; } = new();

        static GlobalSystemMemory()
        {
            // Пример инициализации аппаратных портов по умолчанию
            Variables["DO_1"] = new VariableStorage { Name = "DO_1", TypeName = "Bool", RawValue = "FALSE", Comment = "Main Valve" };
            Variables["DI_1"] = new VariableStorage { Name = "DI_1", TypeName = "Bool", RawValue = "FALSE", Comment = "Safety Sensor" };
        }
    }
    public class ProgramContext
    {
        public string ProgramName { get; set; }

        // ИСТИННО ЛОКАЛЬНЫЕ переменные (видны только в рамках этого файла)
        public Dictionary<string, VariableStorage> LocalVariables { get; set; } = new();

        // Тело программы (Строки с формами)
        public List<Line> CodeLines { get; set; } = new();

        // Индексы фокуса для восстановления UI
        public int CurrentLineIndex { get; set; } = 0;
        public int CurrentFieldIndex { get; set; } = 0;

        public ProgramContext(string name)
        {
            ProgramName = name;
        }
    }
    public class VariableStorage
    {
        public required string Name { get; set; }
        public required string TypeName { get; set; } // "Number", "Bool", "String" или имя кастомной структуры

        // Значение всегда храним в строковом виде для универсальности и простоты JSON-сериализации
        public  string RawValue { get; set; } = string.Empty;

        // Поля для поддержки массивов/списков (опционально)
        public bool IsArray { get; set; } = false;
        public int ArraySize { get; set; } = 0;

        // Поля для поддержки промышленных фич (Алиасы / Оператор связывания <=>)
        public bool IsReference { get; set; } = false;
        public string ReferenceTarget { get; set; } = string.Empty; // Имя переменной или ID железного порта, на который мы ссылаемся

        // Комментарий (Лейбл), привязанный к переменной/регистру
        public string Comment { get; set; } = string.Empty;
    }
}