using Director.Avalonia.Services;
using Director.Core.Protocol;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.Tests;

public class DirectorClientTests
{
    private readonly ILogger<DirectorClient> _logger;

    public DirectorClientTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DirectorClient>();
    }

    [Fact]
    public void DirectorClient_InitialState_IsNotConnected()
    {
        using var client = new DirectorClient(_logger);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public void DirectorClient_Dispose_DoesNotThrow()
    {
        using var client = new DirectorClient(_logger);
        // Should not throw
    }

    [Fact]
    public async Task DirectorClient_SendCommand_WhenNotConnected_DoesNotThrow()
    {
        using var client = new DirectorClient(_logger);
        var command = new DirectorCommand("test", "test", new { });
        
        // Should not throw, just silently fail
        await client.SendCommandAsync(command);
    }
}

public class NexoCommandServiceTests
{
    private readonly NexoCommandService _service = new();

    [Fact]
    public void CreateValidationCommand_ReturnsValidCommand()
    {
        var command = _service.CreateValidationCommand();
        
        Assert.Equal(CommandTypes.RunNexo, command.Type);
        Assert.NotNull(command.Payload);
    }

    [Fact]
    public void CreateAnalysisCommand_ReturnsValidCommand()
    {
        var command = _service.CreateAnalysisCommand();
        
        Assert.Equal(CommandTypes.RunNexo, command.Type);
        Assert.NotNull(command.Payload);
    }

    [Fact]
    public void CreateListAgentsCommand_ReturnsValidCommand()
    {
        var command = _service.CreateListAgentsCommand();
        
        Assert.Equal(CommandTypes.RunNexo, command.Type);
        Assert.NotNull(command.Payload);
    }

    [Fact]
    public void CreateCustomNexoCommand_WithValidInput_ReturnsValidCommand()
    {
        var command = _service.CreateCustomNexoCommand("test", new[] { "arg1", "arg2" });
        
        Assert.Equal(CommandTypes.RunNexo, command.Type);
        Assert.NotNull(command.Payload);
    }
}

public class TokenServiceTests
{
    private readonly TokenService _service = new();

    [Fact]
    public void IsTokenValid_WithValidToken_ReturnsTrue()
    {
        var validToken = "1234567890123456";
        Assert.True(_service.IsTokenValid(validToken));
    }

    [Fact]
    public void IsTokenValid_WithInvalidToken_ReturnsFalse()
    {
        Assert.False(_service.IsTokenValid(""));
        Assert.False(_service.IsTokenValid("short"));
        Assert.False(_service.IsTokenValid(null!));
    }
}

public class DiffServiceTests
{
    private readonly DiffService _service = new();

    [Fact]
    public void GenerateDiff_WithIdenticalStrings_ReturnsNoChanges()
    {
        var result = _service.GenerateDiff("same", "same");
        Assert.Equal("No changes", result);
    }

    [Fact]
    public void GenerateDiff_WithDifferentStrings_ReturnsDiff()
    {
        var result = _service.GenerateDiff("old", "new");
        Assert.Contains("old", result);
        Assert.Contains("new", result);
    }

    [Fact]
    public void HasChanges_WithIdenticalStrings_ReturnsFalse()
    {
        Assert.False(_service.HasChanges("same", "same"));
    }

    [Fact]
    public void HasChanges_WithDifferentStrings_ReturnsTrue()
    {
        Assert.True(_service.HasChanges("old", "new"));
    }
}
