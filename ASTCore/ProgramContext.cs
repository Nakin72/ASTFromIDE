using System.Collections.Generic;
using AstroEditor.Core.ObjectModel; // Подключаем нашу иерархию Line/Form/Field/Token

namespace AstroEditor.Core.Memory
{
    public class ProgramContext
    {
        public string ProgramName { get; set; }

        // Глобальная и статическая память программы (Таблица символов)
        // Ключ — уникальное имя переменной (например, "flag", "boo", "DO_2")
        public Dictionary<string, VariableStorage> Variables { get; set; } = new();

        // Тело программы — упорядоченный список строк с формами
        public List<Line> CodeLines { get; set; } = new();

        // Состояние интерфейса для восстановления работы
        public int CurrentLineIndex { get; set; } = 0; // На какой строке стоял оператор
        public int CurrentFieldIndex { get; set; } = 0; // Какое поле было в фокусе

        public ProgramContext(string name)
        {
            ProgramName = name;
        }
    }
}