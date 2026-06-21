// AstroEditor.Core/Data/Services/IProjectLifecycleService.cs
// Интерфейс сервиса жизненного цикла проекта

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис управления жизненным циклом проекта (инициализация, сохранение, загрузка).
/// </summary>
public interface IProjectLifecycleService
{
    string ProjectFolder { get; }
    bool HasUnsavedChanges { get; }
    
    event Action? OnProjectChanged;
    
    void InitializeNew(string projectFolder);
    void Open(string projectFolder);
    void SaveAll();
    void SaveProgram(string programName);
}
