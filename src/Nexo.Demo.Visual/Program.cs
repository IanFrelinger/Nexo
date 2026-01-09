using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Demo.Bricks.Security;
using Nexo.Demo.Visual;
using Nexo.Infrastructure.Execution;

var builder = Host.CreateApplicationBuilder(args);

// Register Nexo services
builder.Services.AddLogging(b => b.AddConsole());

// Register infrastructure services
builder.Services.AddSingleton<IProviderFactory, ProviderFactory>();

// Register demo bricks
builder.Services.AddSingleton<OWASPScannerBrick>();

// Register registries
builder.Services.AddSingleton<IBrickRegistry>(sp =>
{
    var bricks = new List<Brick>
    {
        sp.GetRequiredService<OWASPScannerBrick>()
    };
    return new BrickRegistry(bricks);
});

// Register demo application
builder.Services.AddSingleton<DemoApplication>();

var host = builder.Build();

// Run the demo
var demo = host.Services.GetRequiredService<DemoApplication>();
await demo.RunAsync(args);

