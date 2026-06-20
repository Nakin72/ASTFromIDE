// AstroEditor.Core/Plugins/PluginAttribute.cs
// Атрибут для маркировки классов как плагинов

namespace AstroEditor.Core.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public string Author { get; set; } = "Unknown";
    public string MinHostVersion { get; set; } = "1.0";

    public PluginAttribute(string name, string version, string description)
    {
        Name = name;
        Version = version;
        Description = description;
    }
}
