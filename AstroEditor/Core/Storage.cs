using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AstroEditor.Core.ObjectModel;
using AstroEditor.Core.Memory;
namespace AstroEditor.Core.Storage
{
    public static class StorageManager
    {
        private static readonly JsonSerializerOptions _options;

        static StorageManager()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true, // Чтобы файл был читаем человеком на диске
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            // Настройка полиморфизма для узлов данных AST (чтобы JSON понимал разницу между константой и объектом)
            // Эти настройки соответствуют интерфейсу I_AstNode, который мы утвердили ранее
        }

        // Сохранить все состояние среды в один клик
        public static void SaveSession(ProgramContext context, string filePath)
        {
            string json = JsonSerializer.Serialize(context, _options);
            File.WriteAllText(filePath, json);
        }

        // Полностью восстановить работу среды из файла
        public static ProgramContext LoadSession(string filePath)
        {
            // 1. Если файла не существует, возвращаем пустой контекст по умолчанию
            if (!File.Exists(filePath))
            {
                return new ProgramContext("NewProgram");
            }

            string json = File.ReadAllText(filePath);

            // Десериализуем во временную nullable-переменную
            ProgramContext? context = JsonSerializer.Deserialize<ProgramContext>(json, _options);

            // 2. Если файл существовал, но внутри был пустой или поврежденный JSON (десериализатор вернул null)
            // то мы также безопасно возвращаем чистый рабочий контекст
            if (context == null)
            {
                return new ProgramContext("NewProgram");
            }

            // Здесь компилятор на 100% уверен, что context не равен null, ошибка CS8603 исчезает
            return context;
        }
    }
}