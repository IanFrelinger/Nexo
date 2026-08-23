using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Core.Domain.Exceptions;
using Ashlar.Infrastructure.Agent.Adapters;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Agent;

/// <summary>Tests for agent executor adapter gap coverage.</summary>
[Collection("ProcessCwd")]
public sealed class AgentExecutorAdapterGapCoverageTests
{
    private static readonly object CwdGate = new();
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var act1 = () => new AgentExecutorAdapter(null!, services);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_succeeds_with_registered_agent_and_input_file()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("GapAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var services = new ServiceCollection();
        services.AddSingleton(mockAgent.Object);
        services.AddSingleton<IArtifactCleanupService>(new NoOpCleanupService());
        var provider = services.BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var dll = Path.Combine(dir, "sample.dll");
            await File.WriteAllBytesAsync(dll, [0x4D, 0x5A]);

            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync("GapAgent", new FileInfo(dll));

            result.Success.Should().BeTrue();
            result.AgentName.Should().Be("GapAgent");
            result.Output.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_agent_missing()
    {
        var adapter = new AgentExecutorAdapter(
            NullLogger<AgentExecutorAdapter>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var act = () => adapter.ExecuteAsync("missing-agent-7f3c", null);
        await act.Should().ThrowAsync<AgentExecutionException>()
            .Where(ex => ex.ErrorCode == ErrorCodes.AgentNotFound);
    }

    [Fact]
    public async Task ExecuteAsync_resolves_agent_case_insensitively_from_di()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("gapagent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync("GapAgent", null);
            result.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_uses_default_assembly_when_input_file_missing()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("DefaultAssemblyAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, "fallback.dll"), [0x4D, 0x5A]);

            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync("DefaultAssemblyAgent", null);

            result.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_uses_default_assembly_when_input_file_does_not_exist()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("MissingInputAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, "fallback.dll"), [0x4D, 0x5A]);
            var missing = new FileInfo(Path.Combine(dir, "missing.dll"));
            missing.Exists.Should().BeFalse();

            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync("MissingInputAgent", missing);

            result.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_runs_with_empty_input_path_when_no_assembly_exists()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("EmptyInputAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentActions.None);

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync("EmptyInputAgent", null);
            result.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_discovers_agent_from_app_domain_without_di()
    {
        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(
                NullLogger<AgentExecutorAdapter>.Instance,
                new ServiceCollection().BuildServiceProvider());

            var result = await adapter.ExecuteAsync(AppDomainGapAgent.AgentName, null);
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be(AppDomainGapAgent.AgentName);
        });
    }

    [Fact]
    public async Task ExecuteAsync_discovers_agent_with_string_constructor()
    {
        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(
                NullLogger<AgentExecutorAdapter>.Instance,
                new ServiceCollection().BuildServiceProvider());

            var result = await adapter.ExecuteAsync(StringCtorGapAgent.ExpectedName, null);
            result.Success.Should().BeTrue();
            result.AgentName.Should().Be(StringCtorGapAgent.ExpectedName);
        });
    }

    [Fact]
    public async Task ExecuteAsync_falls_back_to_app_domain_when_di_resolution_fails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAgent>(_ => throw new InvalidOperationException("di resolution failed"));
        var provider = services.BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var result = await adapter.ExecuteAsync(AppDomainGapAgent.AgentName, null);
            result.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_wraps_timeout_exceptions()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TimeoutAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("agent timed out"));

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var act = () => adapter.ExecuteAsync("TimeoutAgent", null);
            await act.Should().ThrowAsync<AgentExecutionException>()
                .Where(ex => ex.AgentName == "TimeoutAgent" && ex.InnerException is TimeoutException);
        });
    }

    [Fact]
    public async Task ExecuteAsync_wraps_unexpected_execution_exceptions()
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("FailureAgent");
        mockAgent.Setup(a => a.ThinkAsync(It.IsAny<AgentObservation>(), It.IsAny<IToolbox>(), It.IsAny<IAgentMemory>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("agent blew up"));

        var provider = new ServiceCollection()
            .AddSingleton(mockAgent.Object)
            .BuildServiceProvider();

        await RunInTempDir(async dir =>
        {
            var adapter = new AgentExecutorAdapter(NullLogger<AgentExecutorAdapter>.Instance, provider);
            var act = () => adapter.ExecuteAsync("FailureAgent", null);
            await act.Should().ThrowAsync<AgentExecutionException>()
                .Where(ex => ex.AgentName == "FailureAgent"
                    && ex.Message.Contains("Agent execution failed")
                    && ex.InnerException is InvalidOperationException);
        });
    }

    [Fact]
    public async Task ExecuteAsync_agent_not_found_includes_suggestion()
    {
        var adapter = new AgentExecutorAdapter(
            NullLogger<AgentExecutorAdapter>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var act = () => adapter.ExecuteAsync("missing-agent-7f3c", null);
        await act.Should().ThrowAsync<AgentExecutionException>()
            .Where(ex => ex.ErrorCode == ErrorCodes.AgentNotFound
                && ex.Suggestion != null
                && ex.Suggestion.Contains("ashlar agent list", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task RunInTempDir(Func<string, Task> action)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ashlar-agent-gap-" + Guid.NewGuid().ToString("N"))).FullName;
        string original;
        lock (CwdGate)
        {
            original = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(dir);
        }

        try
        {
            /// <summary>Action.</summary>
            await action(dir);
        }
        finally
        {
            lock (CwdGate)
            {
                Directory.SetCurrentDirectory(original);
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    /// <summary>Tests for no op cleanup service.</summary>
    private sealed class NoOpCleanupService : IArtifactCleanupService
    {
        /// <summary>Gets available strategy ids.</summary>
        public IReadOnlyList<string> GetAvailableStrategyIds() => Array.Empty<string>();

        public Task<ArtifactCleanupResult> CleanAsync(
            string? strategyId = null,
            ArtifactCleanupContext? context = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ArtifactCleanupResult("noop", 0, Array.Empty<string>(), Array.Empty<string>()));
    }

    /// <summary>Tests for app domain gap agent.</summary>
    private sealed class AppDomainGapAgent : IAgent
    {
        public const string AgentName = "ExecutorGapAppDomainAgent";

        public string Name => AgentName;

        public Task<AgentActions> ThinkAsync(
            AgentObservation obs,
            IToolbox tools,
            IAgentMemory mem,
            CancellationToken ct) =>
            Task.FromResult(AgentActions.None);
    }

    /// <summary>Tests for string ctor gap agent.</summary>
    private sealed class StringCtorGapAgent : IAgent
    {
        public const string ExpectedName = "ExecutorGapStringCtorAgent";

        /// <summary>String ctor gap agent.</summary>
        public StringCtorGapAgent() => Name = "wrong-default-name";

        public StringCtorGapAgent(string name)
        {
            if (!name.StartsWith("ExecutorGap", StringComparison.Ordinal))
                throw new ArgumentException("Unexpected agent name.", nameof(name));
            Name = name;
        }

        /// <summary>Name.</summary>
        public string Name { get; }

        public Task<AgentActions> ThinkAsync(
            AgentObservation obs,
            IToolbox tools,
            IAgentMemory mem,
            CancellationToken ct) =>
            Task.FromResult(AgentActions.None);
    }
}
