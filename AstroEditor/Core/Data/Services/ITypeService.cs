// AstroEditor.Core/Data/Services/ITypeService.cs
// Интерфейс сервиса типов и форм

using AstroEditor.Core.Forms;
using AstroEditor.Core.Types;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Интерфейс сервиса типов и форм.
/// </summary>
public interface ITypeService
{
    /// <summary>Реестр типов данных.</summary>
    DataTypeRegistry TypeRegistry { get; }
    
    /// <summary>Реестр форм инструкций.</summary>
    FormRegistry FormRegistry { get; }
    
    /// <summary>Зарегистрировать тип.</summary>
    void RegisterType(DataType type);
    
    /// <summary>Зарегистрировать форму.</summary>
    void RegisterForm(FormDefinition form);
    
    /// <summary>Получить тип по ID.</summary>
    DataType? GetTypeById(string id);
    
    /// <summary>Получить форму по ID.</summary>
    FormDefinition? GetFormById(string id);
    
    /// <summary>Инициализировать базовые типы.</summary>
    void InitializePrimitives();
    
    /// <summary>Инициализировать встроенные формы.</summary>
    void InitializeBuiltinForms();
}
