// AstroEditor.Tests/Data/Services/ProjectLifecycleServiceErrorTests.cs
// Тесты обработки ошибок ProjectLifecycleService

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using AstroEditor.Core.Programs;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace AstroEditor.Tests.Data.Services;

public class ProjectLifecycleServiceErrorTests : IDisposable
{
    private readonly string _testFolder;
    private readonly ProjectState _state;
    private readonly IProjectStorage _storage;
    private readonly IProgramService _programService;
    private readonly ProjectLifecycleService _service;

    public ProjectLifecycleServiceErrorTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"AstroErrorTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFolder);
        
        _state = new ProjectState();
        _storage = new ProjectStorageService(_state, NullLogger<ProjectStorageService>.Instance);
        _programService = new ProgramService(_state, NullLogger<ProgramService>.Instance);
        _service = new ProjectLifecycleService(_state, _storage, _programService, NullLogger<ProjectLifecycleService>.Instance);
    }

    [Fact]
    public void InitializeNew_EmptyFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.InitializeNew(""));
        Assert.StartsWith("Project folder cannot be empty", ex.Message);
    }

    [Fact]
    public void InitializeNew_NullFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.InitializeNew(null!));
        Assert.StartsWith("Project folder cannot be empty", ex.Message);
    }

    [Fact]
    public void Open_EmptyFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.Open(""));
        Assert.StartsWith("Project folder cannot be empty", ex.Message);
    }

    [Fact]
    public void SaveAll_NoProjectFolder_ThrowsProjectServiceException()
    {
        // Act & Assert
        var ex = Assert.Throws<ProjectServiceException>(() => _service.SaveAll());
        Assert.Contains("Failed to save project", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void SaveProgram_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "SaveTest");
        _service.InitializeNew(projectFolder);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.SaveProgram(""));
        Assert.StartsWith("Program name cannot be empty", ex.Message);
    }

    [Fact]
    public void SaveProgram_NonExistentProgram_DoesNotThrow()
    {
        // Arrange
        var projectFolder = Path.Combine(_testFolder, "SaveNonExistentTest");
        _service.InitializeNew(projectFolder);

        // Act & Assert
        var ex = Record.Exception(() => _service.SaveProgram("NonExistent"));
        Assert.Null(ex);
    }

    [Fact]
    public void InitializeNew_InvalidPath_DoesNotThrow()
    {
        // Arrange - Directory.CreateDirectory создаёт папки, если они не существуют
        var validNewPath = Path.Combine(_testFolder, "New", "Nested", "Project");

        // Act & Assert - не должно выбросить исключение
        var ex = Record.Exception(() => _service.InitializeNew(validNewPath));
        Assert.Null(ex);
        Assert.True(Directory.Exists(validNewPath));
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
