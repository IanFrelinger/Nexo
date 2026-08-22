using Ashlar.Agents.TestKit;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Client;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Client;
/// <summary>Tests for ashlar client invoke async.</summary>
public sealed class AshlarClientInvokeAsyncTests
{
    [Fact]
    public async Task InvokeAsync_SendsRelativePathAndMethod()
    {
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.OK);
        var services = new ServiceCollection();
        services.AddHttpClient<IAshlarClient, AshlarClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost/", UriKind.Absolute))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var sp = services.BuildServiceProvider();
        var sut = sp.GetRequiredService<IAshlarClient>();

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await sut.InvokeAsync(HttpMethod.Post, "/api/copilot/task", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.ToString().Should().Be("http://localhost/api/copilot/task");
    }

}
