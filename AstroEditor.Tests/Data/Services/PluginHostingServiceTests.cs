// AstroEditor.Tests/Data/Services/PluginHostingServiceTests.cs
// Тесты сервиса управления плагинами

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace AstroEditor.Tests.Data.Services;

public class PluginHostingServiceTests : IDisposable
{
    private readonly string _testFolder;
    private readonly ProjectState _state;
    private readonly PluginHostingService _service;
    private readonly TypeService _typeService;

    public PluginHostingServiceTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"AstroPluginTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFolder);
        
        _state = new ProjectState();
        _service = new PluginHostingService(_state, NullLogger<PluginHostingService>.Instance);
        _typeService = new TypeService(_state, NullLogger<TypeService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Act & Assert
        Assert.NotNull(_service);
        Assert.Null(_service.PluginManager);
    }

    [Fact]
    public void Initialize_ShouldCreatePluginManager()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "InitTest");
        Directory.CreateDirectory(projectFolder);

        // Act
        _service.Initialize(projectFolder, _typeService);

        // Assert
        Assert.NotNull(_service.PluginManager);
    }

    [Fact]
    public void Initialize_ShouldCreatePluginsFolder()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "PluginsFolderTest");
        Directory.CreateDirectory(projectFolder);

        // Act
        _service.Initialize(projectFolder, _typeService);
        _service.LoadAllPlugins();

        // Assert
        Assert.True(Directory.Exists(Path.Combine(projectFolder, "Plugins")));
    }

    [Fact]
    public void LoadAllPlugins_ShouldNotThrow_WhenNoPlugins()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "LoadEmptyTest");
        Directory.CreateDirectory(projectFolder);
        _service.Initialize(projectFolder, _typeService);

        // Act & Assert
        var ex = Record.Exception(() => _service.LoadAllPlugins());
        Assert.Null(ex);
    }

    [Fact]
    public void LoadAllPlugins_ShouldNotThrow_WhenNotInitialized()
    {
        // Act & Assert
        var ex = Record.Exception(() => _service.LoadAllPlugins());
        Assert.Null(ex);
    }

    [Fact]
    public void UnloadAllPlugins_ShouldNotThrow_WhenNotInitialized()
    {
        // Act & Assert
        var ex = Record.Exception(() => _service.UnloadAllPlugins());
        Assert.Null(ex);
    }

    [Fact]
    public void UnloadAllPlugins_ShouldNotThrow_WhenInitialized()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "UnloadTest");
        Directory.CreateDirectory(projectFolder);
        _service.Initialize(projectFolder, _typeService);

        // Act & Assert
        var ex = Record.Exception(() => _service.UnloadAllPlugins());
        Assert.Null(ex);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);
        }
        catch { }
    }
}
