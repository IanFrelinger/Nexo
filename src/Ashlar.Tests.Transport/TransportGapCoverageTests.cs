using FluentAssertions;
using Google.Protobuf;
using Ashlar.Transport.Grpc;
using Xunit;

namespace Ashlar.Tests.Transport;

/// <summary>Tests for transport gap coverage.</summary>
public sealed class TransportGapCoverageTests
{
    [Fact]
    public void AgentTransportReflection_exposes_proto_descriptor()
    {
        var descriptor = AgentTransportReflection.Descriptor;
        descriptor.Should().NotBeNull();
        descriptor.Name.Should().Be("Protos/agent_transport.proto");
        descriptor.MessageTypes.Should().Contain(t => t.ClrType == typeof(InvokeRequest));
        descriptor.MessageTypes.Should().Contain(t => t.ClrType == typeof(HealthRequest));
    }

    [Fact]
    public void HealthRequest_clone_merge_and_binary_round_trip()
    {
        var original = new HealthRequest();
        var clone = original.Clone();
        clone.Equals(original).Should().BeTrue();
        original.ToString().Should().NotBeNullOrWhiteSpace();

        var bytes = original.ToByteArray();
        var parsed = HealthRequest.Parser.ParseFrom(bytes);
        parsed.Equals(original).Should().BeTrue();

        var merged = new HealthRequest();
        merged.MergeFrom(bytes);
        merged.Equals(original).Should().BeTrue();
    }

    [Fact]
    public void HealthResponse_sets_fields_and_round_trips()
    {
        var response = new HealthResponse
        {
            IsHealthy = true,
            TransportType = "gRPC",
            DiagnosticMessage = "ok",
        };

        response.CalculateSize().Should().BeGreaterThan(0);
        response.Clone().DiagnosticMessage.Should().Be("ok");

        var bytes = response.ToByteArray();
        var parsed = HealthResponse.Parser.ParseFrom(bytes);
        parsed.IsHealthy.Should().BeTrue();
        parsed.TransportType.Should().Be("gRPC");
        parsed.DiagnosticMessage.Should().Be("ok");

        var merged = new HealthResponse();
        merged.MergeFrom(response);
        merged.Equals(response).Should().BeTrue();
    }

    [Fact]
    public void InvokeRequest_sets_all_fields_and_round_trips()
    {
        var request = new InvokeRequest
        {
            AgentName = "agent-1",
            CorrelationId = "corr-1",
            SpanId = "span-1",
            TargetEndpoint = "http://127.0.0.1:5000",
            TimeoutMs = 1500,
            MaxRetries = 2,
        };
        request.Payload["payloadKey"] = """{"n":1}""";
        request.Metadata["domain"] = "General";

        request.CalculateSize().Should().BeGreaterThan(0);
        request.Clone().AgentName.Should().Be("agent-1");

        var other = new InvokeRequest();
        other.MergeFrom(request);
        other.Payload["payloadKey"].Should().Be("""{"n":1}""");
        other.Metadata["domain"].Should().Be("General");

        var parsed = InvokeRequest.Parser.ParseFrom(request.ToByteArray());
        parsed.AgentName.Should().Be("agent-1");
        parsed.TimeoutMs.Should().Be(1500);
        parsed.MaxRetries.Should().Be(2);
    }

    [Fact]
    public void InvokeResponse_sets_all_fields_and_round_trips()
    {
        var response = new InvokeResponse
        {
            Success = false,
            CorrelationId = "corr-2",
            SpanId = "span-2",
            ErrorMessage = "boom",
            ErrorCode = "GRPC_ERROR",
        };
        response.Output["result"] = """{"ok":false}""";

        response.CalculateSize().Should().BeGreaterThan(0);
        response.Clone().ErrorCode.Should().Be("GRPC_ERROR");

        var merged = new InvokeResponse();
        merged.MergeFrom(response);
        merged.Output["result"].Should().Be("""{"ok":false}""");

        using var stream = new MemoryStream();
        response.WriteTo(stream);
        var parsed = InvokeResponse.Parser.ParseFrom(stream.ToArray());
        parsed.Success.Should().BeFalse();
        parsed.ErrorMessage.Should().Be("boom");
    }

    [Fact]
    public void InvokeRequest_equality_and_hash_code_cover_message_contract()
    {
        var left = new InvokeRequest { AgentName = "a", CorrelationId = "c" };
        var right = new InvokeRequest { AgentName = "a", CorrelationId = "c" };
        left.Equals(right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());

        right.AgentName = "b";
        left.Equals(right).Should().BeFalse();
        left.Equals((object?)right).Should().BeFalse();
    }

    [Fact]
    public void HealthResponse_parser_handles_empty_message()
    {
        var empty = new HealthResponse();
        var parsed = HealthResponse.Parser.ParseFrom(empty.ToByteArray());
        parsed.IsHealthy.Should().BeFalse();
        parsed.TransportType.Should().BeEmpty();
        parsed.DiagnosticMessage.Should().BeEmpty();
    }
}
