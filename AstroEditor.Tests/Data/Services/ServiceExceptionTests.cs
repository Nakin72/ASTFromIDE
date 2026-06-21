// AstroEditor.Tests/Data/Services/ServiceExceptionTests.cs
// Тесты исключений сервисов

using Xunit;
using AstroEditor.Core.Data.Services;

namespace AstroEditor.Tests.Data.Services;

public class ServiceExceptionTests
{
    [Fact]
    public void ProjectServiceException_WithMessage_CreatesException()
    {
        // Arrange & Act
        var ex = new ProjectServiceException("Test message");

        // Assert
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void ProjectServiceException_WithInner_CreatesException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var ex = new ProjectServiceException("Outer", inner);

        // Assert
        Assert.Equal("Outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void PluginServiceException_WithMessage_CreatesException()
    {
        // Arrange & Act
        var ex = new PluginServiceException("Test message");

        // Assert
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void PluginServiceException_WithInner_CreatesException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var ex = new PluginServiceException("Outer", inner);

        // Assert
        Assert.Equal("Outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void InterpreterServiceException_WithMessage_CreatesException()
    {
        // Arrange & Act
        var ex = new InterpreterServiceException("Test message");

        // Assert
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void InterpreterServiceException_WithInner_CreatesException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var ex = new InterpreterServiceException("Outer", inner);

        // Assert
        Assert.Equal("Outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
