using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;

namespace Nexo.Tests.E2E;

public class OrchestratorSmokeTests
{
    private sealed record EchoIn(string Message);
    private sealed record EchoOut(string Message);

    private sealed class EchoCommand : ICommand<EchoIn, EchoOut>
    {
        public ValueTask<EchoOut> ExecuteAsync(EchoIn input, CancellationToken ct)
            => ValueTask.FromResult(new EchoOut($"ok:{input.Message}"));
    }

    private sealed class EchoPostValidator : IPostValidator<EchoOut>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(EchoOut output, CancellationToken ct)
            => ValueTask.FromResult(output.Message.StartsWith("ok:")
                ? (true, (string?)null)
                : (false, "missing ok prefix"));
    }

    [Fact]
    public async Task Orchestrator_RunAsync_WithLocalCommand_ShouldReturnSuccess()
    {
        // Arrange
        var orchestrator = new GenericCommandOrchestrator(
            new[] { new NoopPreValidator() },
            new object[] { new EchoPostValidator() });

        var cmd = new EchoCommand();
        var input = new EchoIn("smoke");

        // Act
        var result = await orchestrator.RunAsync(cmd, input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}

