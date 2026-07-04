using FluentAssertions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using Xunit;

namespace Nexo.Tests.Application;

/// <summary>Tests for generic command orchestrator gap coverage.</summary>
public class GenericCommandOrchestratorGapCoverageTests
{
    [Fact]
    public async Task RunAsync_succeeds_when_all_validators_pass()
    {
        var sut = new GenericCommandOrchestrator(
            new IPreValidator[] { new AlwaysPassPreValidator() },
            new object[] { new AlwaysPassPostValidator<string>() },
            new SequentialLoopKernel());

        var result = await sut.RunAsync(new EchoCommand(), "hello", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_returns_pre_validation_failure()
    {
        var sut = new GenericCommandOrchestrator(
            new IPreValidator[] { new FailPreValidator("bad input") },
            Array.Empty<object>(),
            new SequentialLoopKernel());

        var result = await sut.RunAsync(new EchoCommand(), "hello", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("pre-validation failed");
    }

    [Fact]
    public async Task RunAsync_runs_multiple_pre_and_post_validators()
    {
        var sut = new GenericCommandOrchestrator(
            new IPreValidator[] { new AlwaysPassPreValidator(), new AlwaysPassPreValidator() },
            new object[] { new AlwaysPassPostValidator<string>(), new AlwaysPassPostValidator<string>() },
            new SequentialLoopKernel());

        var result = await sut.RunAsync(new EchoCommand(), "payload", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void OrchestrationResult_record_exposes_success_and_message()
    {
        var ok = new OrchestrationResult(true);
        ok.Success.Should().BeTrue();
        ok.Message.Should().BeNull();

        var fail = new OrchestrationResult(false, "denied");
        fail.Success.Should().BeFalse();
        fail.Message.Should().Be("denied");
    }

    [Fact]
    public async Task RunAsync_returns_post_validation_failure()
    {
        var sut = new GenericCommandOrchestrator(
            Array.Empty<IPreValidator>(),
            new object[] { new FailPostValidator<string>("bad output") },
            new SequentialLoopKernel());

        var result = await sut.RunAsync(new EchoCommand(), "hello", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("post-validation failed");
    }

    /// <summary>CLI command for echo.</summary>
    private sealed class EchoCommand : ICommand<string, string>
    {
        /// <summary>Execute async.</summary>
        /// <param name="input">Input.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public ValueTask<string> ExecuteAsync(string input, CancellationToken cancellationToken) =>
            ValueTask.FromResult(input);
    }

    /// <summary>Always pass pre validator.</summary>
    private sealed class AlwaysPassPreValidator : IPreValidator
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct) =>
            ValueTask.FromResult((true, (string?)null));
    }

    /// <summary>Fail pre validator.</summary>
    private sealed class FailPreValidator(string reason) : IPreValidator
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(object input, CancellationToken ct) =>
            ValueTask.FromResult<(bool, string?)>((false, reason));
    }

    /// <summary>Always pass post validator.</summary>
    private sealed class AlwaysPassPostValidator<TOut> : IPostValidator<TOut>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(TOut output, CancellationToken ct) =>
            ValueTask.FromResult((true, (string?)null));
    }

    /// <summary>Fail post validator.</summary>
    private sealed class FailPostValidator<TOut>(string reason) : IPostValidator<TOut>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(TOut output, CancellationToken ct) =>
            ValueTask.FromResult<(bool, string?)>((false, reason));
    }
}
