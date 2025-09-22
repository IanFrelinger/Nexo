using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Agent.Tools.Builtin;
using Nexo.Agent.Abstractions;
using Xunit;

namespace Nexo.Agent.Tools.Builtin.Tests;

public class FileReadToolTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly FileReadTool _tool;

    public FileReadToolTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockFileSystem = new Mock<IFileSystem>();
        _tool = new FileReadTool();
    }

    [Fact]
    public void Manifest_ShouldHaveCorrectProperties()
    {
        // Act & Assert
        _tool.Manifest.Id.Should().Be("tool.file.read");
        _tool.Manifest.Name.Should().Be("File Read");
        _tool.Manifest.Description.Should().Contain("Read text or binary files");
        _tool.Manifest.RequiredPermissions.Should().Be(ToolPermissions.FileRead);
        _tool.Manifest.Capabilities.Should().Contain("file");
        _tool.Manifest.Capabilities.Should().Contain("read");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTextFile_ShouldReturnContent()
    {
        // Arrange
        var input = new FileReadInput("test.txt", false);
        var context = CreateToolContext();
        var expectedContent = "Hello, World!";

        _mockFileSystem.Setup(x => x.FileExists("test.txt")).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync("test.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);
        _mockFileSystem.Setup(x => x.GetFileSize("test.txt")).Returns(expectedContent.Length);

        // Act
        var result = await _tool.ExecuteAsync(input, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Be(expectedContent);
        result.FileSize.Should().Be(expectedContent.Length);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidBinaryFile_ShouldReturnBase64Content()
    {
        // Arrange
        var input = new FileReadInput("test.bin", true);
        var context = CreateToolContext();
        var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        var expectedBase64 = Convert.ToBase64String(binaryData);

        _mockFileSystem.Setup(x => x.FileExists("test.bin")).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllBytesAsync("test.bin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(binaryData);

        // Act
        var result = await _tool.ExecuteAsync(input, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Be(expectedBase64);
        result.FileSize.Should().Be(binaryData.Length);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var input = new FileReadInput("nonexistent.txt", false);
        var context = CreateToolContext();

        _mockFileSystem.Setup(x => x.FileExists("nonexistent.txt")).Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(input, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Content.Should().BeNull();
        result.Error.Should().Contain("File not found");
        result.FileSize.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithFileSystemError_ShouldReturnError()
    {
        // Arrange
        var input = new FileReadInput("error.txt", false);
        var context = CreateToolContext();

        _mockFileSystem.Setup(x => x.FileExists("error.txt")).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync("error.txt", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Access denied"));

        // Act
        var result = await _tool.ExecuteAsync(input, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Content.Should().BeNull();
        result.Error.Should().Contain("Access denied");
        result.FileSize.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonInput_ShouldDeserializeCorrectly()
    {
        // Arrange
        var inputJson = """{"filePath": "test.json", "readAsBinary": false}""";
        var context = CreateToolContext();
        var expectedContent = """{"message": "test"}""";

        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);
        _mockFileSystem.Setup(x => x.GetFileSize(It.IsAny<string>())).Returns(expectedContent.Length);

        // Act
        var result = await _tool.ExecuteAsync(inputJson, context);

        // Assert
        result.Should().NotBeNull();
        // The result should be a JSON string containing the FileReadOutput
        result.Should().Contain("Success");
        result.Should().Contain("true");
        result.Should().Contain("message");
        result.Should().Contain("test");
    }

    private ToolContext CreateToolContext()
    {
        return new ToolContext(
            Logger: _mockLogger.Object,
            CancellationToken: CancellationToken.None,
            Timeout: TimeSpan.FromMinutes(5),
            Permissions: ToolPermissions.FileRead,
            FileSystem: _mockFileSystem.Object,
            WorkingDirectory: Environment.CurrentDirectory
        );
    }
}
