using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Transport.Grpc;
using Xunit;

namespace Nexo.Tests.Transport;

/// <summary>Tests for grpc agent transport barrier ambient.</summary>
[Collection("GrpcTransportEnvironment")]
public sealed class GrpcAgentTransportBarrierAmbientTests
{
    [Fact]
    public void BuildHeaders_includes_barrier_from_ambient_when_scoped_accessor_initialized()
    {
        using var env = new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development");
        var hierarchy = new BarrierHierarchy(
        [
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1)
        ]);
        var options = Options.Create(new BarrierOptions
        {
            Levels = ["public", "internal"],
            RequireExplicitBarrier = false
        });
        var ambient = new BarrierContextAmbient();
        var scoped = new ScopedBarrierContextAccessor(hierarchy, options, ambient);
        var context = BarrierContext.Create(
            "internal",
            BarrierAuthoritySource.Cli,
            issuedTo: "*",
            correlationId: "corr-ambient",
            hierarchy);
        scoped.Initialize(context);

        var factory = new DefaultGrpcChannelFactory(
            Options.Create(new GrpcTransportOptions { AllowInsecure = true }),
            NullLogger<DefaultGrpcChannelFactory>.Instance);
        using var transport = new GrpcAgentTransport(
            factory,
            NullLogger<GrpcAgentTransport>.Instance,
            ambient);

        var request = new AgentInvocationRequest(
            AgentName: "agent-1",
            CorrelationId: "corr-ambient",
            Options: new AgentInvocationOptions(
                Timeout: TimeSpan.FromSeconds(1),
                MaxRetries: 0,
                TargetEndpoint: "http://127.0.0.1:9"));

        var buildHeaders = typeof(GrpcAgentTransport)
            .GetMethod("BuildHeaders", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        buildHeaders.Should().NotBeNull();
        var headers = (Grpc.Core.Metadata)buildHeaders!.Invoke(transport, [request])!;
        headers.GetValue("x-nexo-barrier")!.Should().Be("internal");
    }

    /// <summary>Environment variable scope.</summary>
    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _key;
        private readonly string? _priorValue;

        public EnvironmentVariableScope(string key, string? value)
        {
            _key = key;
            _priorValue = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        /// <summary>Dispose.</summary>
        public void Dispose() => Environment.SetEnvironmentVariable(_key, _priorValue);
    }
}
