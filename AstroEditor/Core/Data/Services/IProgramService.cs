// AstroEditor.Core/Data/Services/IProgramService.cs
// Интерфейс сервиса программ

using AstroEditor.Core.Programs;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Интерфейс сервиса программ — создание, управление программами.
/// </summary>
public interface IProgramService
{
    /// <summary>Все программы проекта.</summary>
    IReadOnlyDictionary<string, AstroProgram> Programs { get; }
    
    /// <summary>Добавить программу.</summary>
    void AddProgram(AstroProgram program);
    
    /// <summary>Удалить программу.</summary>
    bool RemoveProgram(string name);
    
    /// <summary>Получить программу по имени.</summary>
    AstroProgram? GetProgram(string name);
    
    /// <summary>Создать новую программу.</summary>
    AstroProgram CreateProgram(string name, string author = "", string description = "");
    
    /// <summary>Событие изменения списка программ.</summary>
    event Action OnProgramsChanged;
}
