using Ashlar.Agents.TestKit;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using FluentAssertions;
using Ashlar.Infrastructure.Testing;
using Ashlar.Infrastructure.Testing.Docker;
using Ashlar.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Testing;

/// <summary>Tests for remote execution platform gap coverage.</summary>
public sealed class RemoteExecutionPlatformGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_http_client()
    {
        var act = () => new RemoteExecutionPlatform(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public async Task BuildImageAsync_returns_failure_details_from_remote_response()
    {
        var handler = StubHttpMessageHandler.FromSync((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("build", StringComparison.Ordinal))
            {
                /// <summary>Json.</summary>
                /// <param name="dockerfile"">Dockerfile".</param>
                return Json(HttpStatusCode.OK, """{"success":false,"errorMessage":"bad dockerfile","durationMs":25}""");
            }

            /// <summary>Json.</summary>
            return Json(HttpStatusCode.NotFound, "{}");
        });

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://remote.example/") };
        var platform = new RemoteExecutionPlatform(client);

        var result = await platform.BuildImageAsync("Dockerfile", "tag:latest", ".");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("bad dockerfile");
    }

    /// <summary>Json.</summary>
    /// <param name="status">Status.</param>
    /// <param name="json">Json.</param>
    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

}
