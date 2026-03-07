using Microsoft.Extensions.DependencyInjection;
using Nexo.API.Endpoints;
using Nexo.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddNexo(options =>
{
    options.PatternStorePath = builder.Configuration["Nexo:PatternStorePath"];
    options.RegisterBackgroundAgentHostedService = true;
});

var app = builder.Build();

app.MapNexoEndpoints();

app.Run();
