using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions.Routing;
using Ashlar.Orchestration.Routing;
using Xunit;

namespace Ashlar.Tests.Orchestration.Routing;

/// <summary>Tests for composite endpoint router.</summary>
public sealed class CompositeEndpointRouterTests
{
    [Fact]
    public async Task ResolveAsync_NoRegisteredEndpoints_ReturnsNullForInProcess()
    {
        var router = CreateRouter([]);

        var resolved = await router.ResolveAsync(CreateContext());

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_SingleHealthyEndpointWithMatchingCapability_ReturnsEndpoint()
    {
        var router = CreateRouter([
            Endpoint("https://a", "a", ["CodeGeneration"], ["public"], "us-east-1", 5, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext(requiredCapabilities: ["CodeGeneration"]));

        resolved.Should().Be("https://a");
    }

    [Fact]
    public async Task ResolveAsync_CapabilityMismatch_ReturnsNullForInProcessFallback()
    {
        var router = CreateRouter([
            Endpoint("https://a", "a", ["SecurityAnalysis"], ["public"], "us-east-1", 5, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext(requiredCapabilities: ["CodeGeneration"]));

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_BarrierLevelMustBeAccepted_WhenEndpointsRestrict()
    {
        var router = CreateRouter([
            Endpoint("https://a", "a", ["CodeGeneration"], ["internal"], "us-east-1", 2, true),
            Endpoint("https://b", "b", ["CodeGeneration"], ["public"], "us-east-1", 1, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext(barrierLevel: "internal"));

        resolved.Should().Be("https://a");
    }

    [Fact]
    public async Task ResolveAsync_AllCandidatesUnhealthy_ThrowsEndpointUnavailable()
    {
        var router = CreateRouter([
            Endpoint("https://a", "a", ["CodeGeneration"], ["public"], "us-east-1", 1, false),
            Endpoint("https://b", "b", ["CodeGeneration"], ["public"], "us-east-1", 2, false)
        ]);

        var act = async () => await router.ResolveAsync(CreateContext());

        var ex = await act.Should().ThrowAsync<EndpointUnavailableException>();
        ex.Which.UnhealthyEndpoints.Should().Contain(["https://a", "https://b"]);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMatches_LowerPriorityWins()
    {
        var router = CreateRouter([
            Endpoint("https://slow", "slow", ["CodeGeneration"], ["public"], "us-east-1", 10, true),
            Endpoint("https://fast", "fast", ["CodeGeneration"], ["public"], "us-east-1", 1, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext());

        resolved.Should().Be("https://fast");
    }

    [Fact]
    public async Task ResolveAsync_PriorityTiebreak_AlphabeticalNameWins()
    {
        var router = CreateRouter([
            Endpoint("https://z", "zulu", ["CodeGeneration"], ["public"], "us-east-1", 1, true),
            Endpoint("https://a", "alpha", ["CodeGeneration"], ["public"], "us-east-1", 1, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext());

        resolved.Should().Be("https://a");
    }

    [Fact]
    public async Task ResolveAsync_PreferredRegionWinsOverHigherPriorityOutOfRegion()
    {
        var router = CreateRouter([
            Endpoint("https://preferred", "preferred", ["CodeGeneration"], ["public"], "us-west-2", 50, true),
            Endpoint("https://priority", "priority", ["CodeGeneration"], ["public"], "us-east-1", 1, true)
        ]);

        var resolved = await router.ResolveAsync(CreateContext(preferredRegion: "us-west-2"));

        resolved.Should().Be("https://preferred");
    }

    [Fact]
    public async Task ResolveAsync_LogsCorrelationIdInDebugDecision()
    {
        const string correlationId = "corr-xyz";
        var logger = new CaptureLogger<CompositeEndpointRouter>();
        var router = CreateRouter([
            Endpoint("https://a", "a", ["CodeGeneration"], ["public"], "us-east-1", 1, true)
        ], logger);

        _ = await router.ResolveAsync(CreateContext(correlationId: correlationId));

        logger.Messages.Should().Contain(message => message.Contains(correlationId, StringComparison.Ordinal));
    }

    private static CompositeEndpointRouter CreateRouter(
        IReadOnlyList<EndpointDescriptor> endpoints,
        ILogger<CompositeEndpointRouter>? logger = null)
    {
        var registry = new TestEndpointRegistry(endpoints);
        var policies = new IRoutingPolicy[]
        {
            new CapabilityRoutingPolicy(),
            new BarrierRoutingPolicy(),
            new GeographicAffinityPolicy(NullLogger<GeographicAffinityPolicy>.Instance),
            new HealthFilterPolicy(),
            new PrioritySelectionPolicy()
        };

        return new CompositeEndpointRouter(
            registry,
            policies,
            logger ?? NullLogger<CompositeEndpointRouter>.Instance);
    }

    private static EndpointRoutingContext CreateContext(
        string? barrierLevel = null,
        string? preferredRegion = null,
        string correlationId = "corr-1",
        IReadOnlyList<string>? requiredCapabilities = null)
    {
        return new EndpointRoutingContext(
            AgentName: "agent-1",
            CorrelationId: correlationId,
            RequiredCapabilities: requiredCapabilities ?? [],
            BarrierLevel: barrierLevel ?? "public",
            PreferredRegion: preferredRegion,
            AgentMetadata: new Dictionary<string, object?>());
    }

    private static EndpointDescriptor Endpoint(
        string endpoint,
        string name,
        IReadOnlyList<string> capabilities,
        IReadOnlyList<string> acceptedBarrierLevels,
        string? region,
        int priority,
        bool isHealthy)
    {
        return new EndpointDescriptor(
            Endpoint: endpoint,
            Name: name,
            SupportedCapabilities: capabilities,
            AcceptedBarrierLevels: acceptedBarrierLevels,
            Region: region,
            Priority: priority,
            IsHealthy: isHealthy);
    }

    /// <summary>Test endpoint registry.</summary>
    private sealed class TestEndpointRegistry : IEndpointRegistry
    {
        private readonly List<EndpointDescriptor> _descriptors;

        public TestEndpointRegistry(IReadOnlyList<EndpointDescriptor> descriptors)
        {
            _descriptors = descriptors.ToList();
        }

        /// <summary>Gets all.</summary>
        public IReadOnlyList<EndpointDescriptor> GetAll() => _descriptors.ToList();

        public void Register(EndpointDescriptor descriptor)
        {
            var index = _descriptors.FindIndex(e => string.Equals(e.Endpoint, descriptor.Endpoint, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                _descriptors[index] = descriptor;
            else
                _descriptors.Add(descriptor);
        }

        public void UpdateHealth(string endpoint, bool isHealthy)
        {
            var index = _descriptors.FindIndex(e => string.Equals(e.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                _descriptors[index] = _descriptors[index] with { IsHealthy = isHealthy };
        }
    }

    /// <summary>Capture logger.</summary>
    private sealed class CaptureLogger<T> : ILogger<T>
    {
        /// <summary>Messages.</summary>
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Returns whether  enabled.</summary>
        /// <param name="logLevel">Log level.</param>
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
