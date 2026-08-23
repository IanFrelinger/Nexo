using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for mesh security middleware.</summary>
public sealed class MeshSecurityMiddlewareTests
{
    [Fact]
    public async Task Mesh_post_without_token_returns_401_when_mesh_token_configured()
    {
        var app = BuildApp(new MeshSecurityOptions { MeshMutatingToken = "secret-mesh" });
        var ctx = CreateContext(HttpMethods.Post, "/api/mesh/tasks", body: "{}");
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Mesh_post_with_valid_token_proceeds()
    {
        var app = BuildApp(new MeshSecurityOptions { MeshMutatingToken = "secret-mesh" });
        var ctx = CreateContext(HttpMethods.Post, "/api/mesh/tasks", body: "{}");
        ctx.Request.Headers["X-Ashlar-Mesh-Token"] = "secret-mesh";
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Brick_execute_accepts_mesh_header_when_only_mesh_mutating_token_set()
    {
        var app = BuildApp(new MeshSecurityOptions { MeshMutatingToken = "shared-secret" });
        var ctx = CreateContext(HttpMethods.Post, "/api/bricks/foo/execute", body: "{}");
        ctx.Request.Headers["X-Ashlar-Mesh-Token"] = "shared-secret";
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Brick_execute_requires_brick_token_when_set()
    {
        var app = BuildApp(new MeshSecurityOptions { BrickExecuteToken = "brick-only" });
        var ctx = CreateContext(HttpMethods.Post, "/api/bricks/foo/execute", body: "{}");
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);

        ctx = CreateContext(HttpMethods.Post, "/api/bricks/foo/execute", body: "{}");
        ctx.Request.Headers["X-Ashlar-Brick-Execute-Token"] = "brick-only";
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Content_length_over_max_returns_413()
    {
        var app = BuildApp(new MeshSecurityOptions { MaxJsonBodyBytes = 10 });
        var ctx = CreateContext(HttpMethods.Post, "/api/mesh/tasks", body: new string('x', 100));
        ctx.Request.ContentLength = 100;
        await app.Invoke(ctx);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, ctx.Response.StatusCode);
    }

    private static RequestDelegate BuildApp(MeshSecurityOptions opts)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(opts));
        services.AddSingleton<ILogger<MeshSecurityMiddleware>>(_ => NullLogger<MeshSecurityMiddleware>.Instance);
        var sp = services.BuildServiceProvider();

        var builder = new ApplicationBuilder(sp);
        builder.UseMiddleware<MeshSecurityMiddleware>();
        builder.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await ctx.Response.WriteAsync("ok");
        });
        return builder.Build();
    }

    private static DefaultHttpContext CreateContext(string method, string path, string body)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.Path = path;
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.2");
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }
}
