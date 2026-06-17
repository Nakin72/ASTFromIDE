using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AstroEditor.Core.v2.IO
{
    public static class ProjectSaveManager
    {
        // Настройки сериализации для красивого и читаемого JSON (с поддержкой наших полиморфных типов)
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Безопасно и атомарно сохраняет объект в JSON-файл с защитой от потери питания.
        /// </summary>
        /// <typeparam name="T">Тип данных (AstroProgram или VariableTable)</typeparam>
        /// <param name="filePath">Целевой путь к файлу (например, "programs/main.json")</param>
        /// <param name="data">Объект для сохранения</param>
        public static void SaveAtomic<T>(string filePath, T data)
        {
            if (data == null) return;

            // Выделяем пути для временного и резервного файлов
            string tempPath = filePath + ".tmp";
            string backupPath = filePath + ".bak";

            try
            {
                // Шаг 1: Сериализуем данные во временный файл с жестким сбросом буферов диска
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
                    {
                        JsonSerializer.Serialize(writer, data, _jsonOptions);
                    }
                    
                    // Заставляем ОС физически отправить байты на пластины HDD/ячейки SSD прямо сейчас
                    fs.Flush(flushToDisk: true); 
                }

                // Шаг 2: Атомарная замена файла средствами ОС
                if (File.Exists(filePath))
                {
                    // Заменяет filePath на tempPath, а старый filePath сохраняет в .bak на случай отката
                    File.Replace(tempPath, filePath, backupPath);
                }
                else
                {
                    // Если основного файла еще нет (первое сохранение), просто переименовываем
                    File.Move(tempPath, filePath);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, чтобы среда не упала, если диск защищен от записи или переполнен
                System.Diagnostics.Debug.WriteLine($"[SaveManager Error]: {ex.Message}");
            }
            finally
            {
                // На всякий случай подчищаем временный файл, если что-то пошло не так
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        /// <summary>
        /// Загрузка данных из файла
        /// </summary>
        public static T? Load<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                // Если основной файл поврежден (например, из-за старого сбоя без теневого копирования),
                // пробуем восстановить данные из резервной .bak копии
                string backupPath = filePath + ".bak";
                if (File.Exists(backupPath))
                {
                    string backupJson = File.ReadAllText(backupPath);
                    return JsonSerializer.Deserialize<T>(backupJson, _jsonOptions);
                }
                return null;
            }
        }
    }
}
