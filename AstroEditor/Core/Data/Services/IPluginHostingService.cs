// AstroEditor.Core/Data/Services/IPluginHostingService.cs
// Интерфейс сервиса управления плагинами

using AstroEditor.Core.Plugins;

namespace AstroEditor.Core.Data.Services;

/// <summary>
/// Сервис управления плагинами (загрузка, выгрузка, регистрация).
/// </summary>
public interface IPluginHostingService
{
    PluginManager? PluginManager { get; }
    
    void Initialize(string projectFolder, ITypeService typeService);
    void LoadAllPlugins();
    void UnloadAllPlugins();
}
