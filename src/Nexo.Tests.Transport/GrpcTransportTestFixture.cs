using System.Net;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;
using Xunit;

namespace Nexo.Tests.Transport;

/// <summary>Grpc transport test fixture.</summary>
internal sealed class GrpcTransportTestFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    private GrpcTransportTestFixture(WebApplication app, string endpoint)
    {
        _app = app;
        Endpoint = endpoint;
    }

    /// <summary>Endpoint.</summary>
    public string Endpoint { get; }

    public static async Task<GrpcTransportTestFixture> StartAsync(
        IAgentTransport localTransport,
        bool requireExplicitBarrier = false)
    {
        var port = GetFreePort();
        var endpoint = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
            });
        });

        builder.Services.AddSingleton<IAgentTransport>(localTransport);
        builder.Services.AddSingleton<BarrierHierarchy>(_ =>
            new BarrierHierarchy([new BarrierLevel("public", 0), new BarrierLevel("internal", 1)]));
        builder.Services.AddSingleton<IOptions<BarrierOptions>>(
            _ => Options.Create(new BarrierOptions
            {
                Levels = ["public", "internal"],
                RequireExplicitBarrier = requireExplicitBarrier
            }));
        builder.Services.AddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
        builder.Services.AddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
        builder.Services.AddNexoGrpcServer();

        var app = builder.Build();
        app.MapNexoGrpcServer();
        await app.StartAsync();

        /// <summary>Grpc transport test fixture.</summary>
        return new GrpcTransportTestFixture(app, endpoint);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
