using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for run pod http client.</summary>
public sealed class RunPodHttpClientTests
{
    [Fact]
    public async Task PollJobStatus_MapsCancelledState_ToFailed()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.AbsolutePath.Should().Contain("/v2/jobs/job-123");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "status": "cancelled",
                  "message": "job cancelled by provider"
                }
                """, Encoding.UTF8, "application/json")
            });
        }))
        {
            BaseAddress = new Uri("https://api.runpod.io/")
        };

        var sut = new RunPodHttpClient(
            httpClient,
            Options.Create(new RunPodBrickConfig()),
            NullLogger<RunPodHttpClient>.Instance);

        var result = await sut.PollJobStatus(new JobHandle
        {
            InstanceId = "instance-1",
            JobId = "job-123"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.State.Should().Be(RunPodJobState.Failed);
        result.Value.IsTerminal.Should().BeTrue();
        result.Value.Message.Should().Contain("cancelled");
    }

    /// <summary>Tests for fake http message handler.</summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
