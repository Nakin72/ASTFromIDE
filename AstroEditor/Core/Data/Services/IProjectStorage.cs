// AstroEditor.Core/Data/Services/IProjectStorage.cs
// Интерфейс хранилища проекта
// ✅ P2-2: Добавлен async метод для Unit of Work

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Интерфейс хранилища проекта — сохранение и загрузка.
/// ✅ P2-2: Поддержка async операций для Unit of Work.
/// </summary>
public interface IProjectStorage
{
    /// <summary>Папка проекта.</summary>
    string ProjectFolder { get; }
    
    /// <summary>Есть ли несохранённые изменения.</summary>
    bool HasUnsavedChanges { get; }
    
    /// <summary>Инициализировать новый проект.</summary>
    void InitializeNew(string projectFolder);
    
    /// <summary>Открыть существующий проект.</summary>
    void Open(string projectFolder);
    
    /// <summary>Сохранить весь проект.</summary>
    void SaveAll();
    
    /// <summary>Сохранить весь проект (async).</summary>
    /// ✅ P2-2: Для использования в Unit of Work
    Task SaveAllAsync();
    
    /// <summary>Сохранить программу.</summary>
    void SaveProgram(string programName);
    
    /// <summary>Событие изменения проекта.</summary>
    event Action OnProjectChanged;
}
