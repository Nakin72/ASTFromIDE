// AstroEditor.Tests/Data/Services/ProgramServiceTests.cs
// Тесты сервиса управления программами

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Data.Services;

public class ProgramServiceTests
{
    private readonly ProgramService _service;
    private readonly ProjectState _state;

    public ProgramServiceTests()
    {
        _state = new ProjectState();
        _service = new ProgramService(_state, NullLogger<ProgramService>.Instance);
    }

    [Fact]
    public void CreateProgram_ShouldCreateProgram()
    {
        // Act
        var program = _service.CreateProgram("TestProgram", "Author", "Description");

        // Assert
        Assert.NotNull(program);
        Assert.Equal("TestProgram", program.Name);
        Assert.Equal("Author", program.Author);
        Assert.Equal("Description", program.Description);
        Assert.Equal("1.0", program.Version);
    }

    [Fact]
    public void CreateProgram_EmptyName_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateProgram("", "Author", "Desc"));
        Assert.Throws<ArgumentException>(() => _service.CreateProgram(null!, "Author", "Desc"));
    }

    [Fact]
    public void AddProgram_ShouldAddToCollection()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");

        // Act
        _service.AddProgram(program);

        // Assert
        Assert.True(_service.Programs.ContainsKey("TestProgram"));
        Assert.Same(program, _service.Programs["TestProgram"]);
    }

    [Fact]
    public void AddProgram_ExistingProgram_ShouldUpdate()
    {
        // Arrange
        var program1 = _service.CreateProgram("TestProgram", "Author1", "Desc1");
        var program2 = _service.CreateProgram("TestProgram", "Author2", "Desc2");
        _service.AddProgram(program1);

        // Act
        _service.AddProgram(program2);

        // Assert
        Assert.Same(program2, _service.Programs["TestProgram"]);
    }

    [Fact]
    public void AddProgram_Null_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.AddProgram(null!));
    }

    [Fact]
    public void AddProgram_ShouldRaiseEvent()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");
        var eventRaised = false;
        _service.OnProgramsChanged += () => eventRaised = true;

        // Act
        _service.AddProgram(program);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void RemoveProgram_Existing_ShouldRemove()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");
        _service.AddProgram(program);
        Assert.True(_service.Programs.ContainsKey("TestProgram"));

        // Act
        var removed = _service.RemoveProgram("TestProgram");

        // Assert
        Assert.True(removed);
        Assert.False(_service.Programs.ContainsKey("TestProgram"));
    }

    [Fact]
    public void RemoveProgram_NonExisting_ShouldReturnFalse()
    {
        // Act
        var removed = _service.RemoveProgram("NonExisting");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void RemoveProgram_ShouldRaiseEvent()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");
        _service.AddProgram(program);
        var eventRaised = false;
        _service.OnProgramsChanged += () => eventRaised = true;

        // Act
        _service.RemoveProgram("TestProgram");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GetProgram_Existing_ShouldReturn()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");
        _service.AddProgram(program);

        // Act
        var retrieved = _service.GetProgram("TestProgram");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Same(program, retrieved);
    }

    [Fact]
    public void GetProgram_NonExisting_ShouldReturnNull()
    {
        // Act
        var retrieved = _service.GetProgram("NonExisting");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Programs_ShouldReturnReadOnly()
    {
        // Arrange
        var program = _service.CreateProgram("TestProgram");
        _service.AddProgram(program);

        // Act
        var programs = _service.Programs;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, AstroEditor.Core.Programs.AstroProgram>>(programs);
        Assert.Equal(1, programs.Count);
    }

    [Fact]
    public void MultiplePrograms_ShouldManageCorrectly()
    {
        // Arrange
        var prog1 = _service.CreateProgram("Program1");
        var prog2 = _service.CreateProgram("Program2");
        var prog3 = _service.CreateProgram("Program3");

        // Act
        _service.AddProgram(prog1);
        _service.AddProgram(prog2);
        _service.AddProgram(prog3);

        // Assert
        Assert.Equal(3, _service.Programs.Count);
        Assert.True(_service.Programs.ContainsKey("Program1"));
        Assert.True(_service.Programs.ContainsKey("Program2"));
        Assert.True(_service.Programs.ContainsKey("Program3"));

        // Act - удалить одну
        _service.RemoveProgram("Program2");

        // Assert
        Assert.Equal(2, _service.Programs.Count);
        Assert.False(_service.Programs.ContainsKey("Program2"));
    }
}
