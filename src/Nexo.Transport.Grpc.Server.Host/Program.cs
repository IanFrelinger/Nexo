// gRPC transport server host entry point.
// Listen address, TLS and the client-side GrpcTransportOptions are documented in docs/GrpcHost.md.
// The loopback default below applies only when nothing else set a URL: ASPNETCORE_URLS, --urls and
// Kestrel__Certificates__Default__* override it per environment. (A "Urls" key in appsettings.json
// would NOT be overridable by ASPNETCORE_URLS - appsettings loads after the host configuration - so
// the default lives here, not there.)
using Nexo.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Nexo.Runtime;
using Nexo.Transport.Grpc;
using Nexo.Transport.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://127.0.0.1:5001");
}

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
