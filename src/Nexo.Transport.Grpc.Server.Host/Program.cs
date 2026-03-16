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
