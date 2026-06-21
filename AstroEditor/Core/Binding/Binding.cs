// AstroEditor.Core/Binding/Binding.cs
// Модель привязки для нового API

namespace AstroEditor.Core.Binding;

/// <summary>
/// Модель привязки между переменными.
/// </summary>
public class Binding
{
    /// <summary>Уникальный идентификатор привязки.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Имя исходной переменной.</summary>
    public string SourceName { get; set; } = string.Empty;
    
    /// <summary>Имя целевой переменной.</summary>
    public string TargetName { get; set; } = string.Empty;
    
    /// <summary>Направление привязки.</summary>
    public BindingDirection Direction { get; set; } = BindingDirection.Bidirectional;
    
    /// <summary>Время создания.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Привязка активна.</summary>
    public bool IsEnabled { get; set; } = true;
}
