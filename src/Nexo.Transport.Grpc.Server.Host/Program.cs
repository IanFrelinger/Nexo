// gRPC transport server host entry point.
// Listen address, TLS and the client-side GrpcTransportOptions are documented in docs/GrpcHost.md;
// defaults live in appsettings.json (Urls = http://127.0.0.1:5001, HTTP/2 only). ASPNETCORE_URLS
// and Kestrel__Certificates__Default__* override them per environment.
using Nexo.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Nexo.Runtime;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2);
});

builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Nexo:GrpcTransport"));
builder.Services.AddNexoRuntimeRouting(builder.Configuration);
builder.Services.AddNexo();
builder.Services.AddNexoGrpcServer();

var app = builder.Build();
app.MapNexoGrpcServer();
app.MapGet("/", () => "Nexo.Transport.Grpc.Server is running.");

app.Run();
