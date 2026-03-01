using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Mesh;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 9: Instance discovery and capability mesh.
/// </summary>
public sealed class MeshCommand : Command
{
    public MeshCommand() : base("mesh", "Discover and advertise capabilities (Block 9).")
    {
        var discoverOpt = new Option<bool>("--discover", () => false, "Discover peer instances");
        var advertiseOpt = new Option<bool>("--advertise", () => false, "Advertise this instance's capabilities");
        var capabilityOpt = new Option<string?>("--capability", "Find peers with capability");

        AddOption(discoverOpt);
        AddOption(advertiseOpt);
        AddOption(capabilityOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var discover = ctx.ParseResult.GetValueForOption(discoverOpt);
            var advertise = ctx.ParseResult.GetValueForOption(advertiseOpt);
            var capability = ctx.ParseResult.GetValueForOption(capabilityOpt);
            await ExecuteAsync(discover, advertise, capability);
        });
    }

    private static async Task ExecuteAsync(bool discover, bool advertise, string? capability)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMeshInfrastructure()
            .BuildServiceProvider();

        if (discover)
        {
            var discovery = services.GetRequiredService<IInstanceDiscovery>();
            var peers = await discovery.DiscoverAsync().ConfigureAwait(false);
            Console.WriteLine($"Discovered {peers.Count} peer(s):");
            foreach (var p in peers)
                Console.WriteLine($"  - {p.PeerId} @ {p.Endpoint} [{string.Join(", ", p.Capabilities)}]");
        }

        if (advertise)
        {
            var adv = services.GetRequiredService<ICapabilityAdvertisement>();
            await adv.AdvertiseAsync(new[] { new Nexo.Core.Application.Mesh.Models.CapabilityDescriptor { Id = "nexo-cli", Name = "Nexo CLI" } }).ConfigureAwait(false);
            Console.WriteLine("Advertised capabilities.");
        }

        if (!string.IsNullOrEmpty(capability))
        {
            var adv = services.GetRequiredService<ICapabilityAdvertisement>();
            var peers = await adv.FindPeersWithCapabilityAsync(capability).ConfigureAwait(false);
            Console.WriteLine($"Peers with '{capability}': {peers.Count}");
            foreach (var p in peers)
                Console.WriteLine($"  - {p.PeerId} @ {p.Endpoint}");
        }

        if (!discover && !advertise && string.IsNullOrEmpty(capability))
            Console.WriteLine("Use --discover, --advertise, or --capability <name>");
        Environment.ExitCode = 0;
    }
}
