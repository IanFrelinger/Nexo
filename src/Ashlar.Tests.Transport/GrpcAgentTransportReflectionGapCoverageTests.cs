using System.Reflection;
using FluentAssertions;
using Grpc.Core;
using Ashlar.Transport.Grpc;
using Xunit;

namespace Ashlar.Tests.Transport;

/// <summary>Tests for grpc agent transport reflection gap coverage.</summary>
public sealed class GrpcAgentTransportReflectionGapCoverageTests
{
    [Theory]
    [InlineData(StatusCode.DeadlineExceeded, "TIMEOUT")]
    [InlineData(StatusCode.Cancelled, "TIMEOUT")]
    [InlineData(StatusCode.NotFound, "AGENT_NOT_FOUND")]
    [InlineData(StatusCode.Unavailable, "TRANSPORT_UNAVAILABLE")]
    [InlineData(StatusCode.PermissionDenied, "GRPC_ERROR_PERMISSIONDENIED")]
    public void MapRpcErrorCode_maps_status_codes(StatusCode statusCode, string expected)
    {
        var mapped = InvokeStatic<string>("MapRpcErrorCode", statusCode);
        mapped.Should().Be(expected);
    }

    [Fact]
    public void DeserializeFromJson_returns_null_for_whitespace_and_raw_string_for_invalid_json()
    {
        InvokeStatic<object?>("DeserializeFromJson", "   ").Should().BeNull();
        InvokeStatic<object?>("DeserializeFromJson", "not-json{{").Should().Be("not-json{{");
    }

    private static T? InvokeStatic<T>(string methodName, params object?[] args)
    {
        var method = typeof(GrpcAgentTransport).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (T?)method!.Invoke(null, args);
    }
}
