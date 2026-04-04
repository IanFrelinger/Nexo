using Microsoft.Extensions.DependencyInjection;
using Nexo.API.Endpoints;
using Nexo.Hosting;
using Nexo.Runtime;
using Nexo.Transport.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.Configure<GrpcTransportOptions>(
    builder.Configuration.GetSection("Nexo:GrpcTransport"));
builder.Services.AddNexoRuntimeRouting(builder.Configuration);
builder.Services.AddNexo(options =>
{
    options.PatternStorePath = builder.Configuration["Nexo:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService = true;
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapNexoEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
