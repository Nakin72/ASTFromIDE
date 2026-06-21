// AstroEditor.Tests/Data/Services/ProjectLifecycleServiceTests.cs
// Тесты сервиса жизненного цикла проекта

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace AstroEditor.Tests.Data.Services;

public class ProjectLifecycleServiceTests : IDisposable
{
    private readonly string _testFolder;
    private readonly ProjectState _state;
    private readonly IProjectStorage _storage;
    private readonly IProgramService _programService;
    private readonly ProjectLifecycleService _service;

    public ProjectLifecycleServiceTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"AstroTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFolder);
        
        _state = new ProjectState();
        _storage = new ProjectStorageService(_state, NullLogger<ProjectStorageService>.Instance);
        _programService = new ProgramService(_state, NullLogger<ProgramService>.Instance);
        _service = new ProjectLifecycleService(_state, _storage, _programService, NullLogger<ProjectLifecycleService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Act & Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public void InitializeNew_ShouldCreateProjectFolder()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "NewProject");

        // Act
        _service.InitializeNew(projectFolder);

        // Assert
        Assert.True(Directory.Exists(projectFolder));
        Assert.Equal(projectFolder, _service.ProjectFolder);
    }

    [Fact]
    public void InitializeNew_ShouldCreateSubfolders()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "NewProject");

        // Act
        _service.InitializeNew(projectFolder);

        // Assert
        Assert.True(Directory.Exists(Path.Combine(projectFolder, "Programs")));
        Assert.True(Directory.Exists(Path.Combine(projectFolder, "Registry")));
    }

    [Fact]
    public void SaveAll_ShouldNotThrow()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "SaveTest");
        _service.InitializeNew(projectFolder);

        // Act & Assert
        var ex = Record.Exception(() => _service.SaveAll());
        Assert.Null(ex);
    }

    [Fact]
    public void SaveProgram_ShouldNotThrow()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "SaveProgramTest");
        _service.InitializeNew(projectFolder);
        _programService.AddProgram(new AstroProgram { Name = "TestProgram" });

        // Act & Assert
        var ex = Record.Exception(() => _service.SaveProgram("TestProgram"));
        Assert.Null(ex);
    }

    [Fact]
    public void Open_ShouldNotThrow()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "OpenTest");
        _service.InitializeNew(projectFolder);
        _service.SaveAll();

        // Act & Assert
        var ex = Record.Exception(() => _service.Open(projectFolder));
        Assert.Null(ex);
    }

    [Fact]
    public void HasUnsavedChanges_ShouldBeFalseAfterSave()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "UnsavedTest");
        _service.InitializeNew(projectFolder);

        // Act
        _service.SaveAll();

        // Assert
        Assert.False(_service.HasUnsavedChanges);
    }

    [Fact]
    public void OnProjectChanged_ShouldRaise()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "EventTest");
        _service.InitializeNew(projectFolder);
        var eventRaised = false;
        _service.OnProjectChanged += () => eventRaised = true;

        // Act
        _programService.AddProgram(new AstroProgram { Name = "TestProgram" });

        // Assert
        Assert.True(eventRaised);
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
