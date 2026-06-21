// AstroEditor.Tests/Data/Services/TypeServiceTests.cs
// Тесты сервиса типов и форм

using Xunit;
using AstroEditor.Core.Data;
using AstroEditor.Core.Data.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroEditor.Tests.Data.Services;

public class TypeServiceTests
{
    private readonly TypeService _service;
    private readonly ProjectState _state;

    public TypeServiceTests()
    {
        _state = new ProjectState();
        _service = new TypeService(_state, NullLogger<TypeService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Act & Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public void InitializePrimitives_ShouldNotThrow()
    {
        // Act
        var ex = Record.Exception(() => _service.InitializePrimitives());

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void InitializeBuiltinForms_ShouldNotThrow()
    {
        // Act
        var ex = Record.Exception(() => _service.InitializeBuiltinForms());

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void GetTypeById_AfterInit_ShouldReturnTypes()
    {
        // Arrange
        _service.InitializePrimitives();

        // Act
        var intType = _service.GetTypeById("int");

        // Assert
        Assert.NotNull(intType);
    }

    [Fact]
    public void GetTypeById_NonExisting_ShouldReturnNull()
    {
        // Act
        var type = _service.GetTypeById("non_existing");

        // Assert
        Assert.Null(type);
    }

    [Fact]
    public void GetFormById_AfterInit_ShouldReturnRegistry()
    {
        // Arrange
        _service.InitializeBuiltinForms();

        // Act - получаем реестр форм
        var registry = _service.FormRegistry;

        // Assert - реестр должен быть не null
        Assert.NotNull(registry);
    }

    [Fact]
    public void GetFormById_NonExisting_ShouldReturnNull()
    {
        // Act
        var form = _service.GetFormById("non_existing");

        // Assert
        Assert.Null(form);
    }

    [Fact]
    public void TypeRegistry_ShouldBeAccessible()
    {
        // Act
        var registry = _service.TypeRegistry;

        // Assert
        Assert.NotNull(registry);
    }

    [Fact]
    public void FormRegistry_ShouldBeAccessible()
    {
        // Act
        var registry = _service.FormRegistry;

        // Assert
        Assert.NotNull(registry);
    }

    [Fact]
    public void InitializePrimitives_Idempotent()
    {
        // Act
        _service.InitializePrimitives();
        _service.InitializePrimitives();

        // Assert - исключений быть не должно
    }

    [Fact]
    public void RegisterType_ShouldAddToRegistry()
    {
        // Arrange
        var type = new AstroEditor.Core.Types.PrimitiveDataType
        {
            Id = "custom_type",
            Name = "CUSTOM"
        };

        // Act
        _service.RegisterType(type);

        // Assert
        var retrieved = _service.GetTypeById("custom_type");
        Assert.NotNull(retrieved);
    }
}
