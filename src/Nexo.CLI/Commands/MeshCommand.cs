using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Mesh;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 9: Instance discovery and capability mesh.
/// P2.3: nexo mesh sync pulls and validates shared adaptations before adoption.
/// </summary>
public sealed class MeshCommand : Command
{
    public MeshCommand() : base("mesh", "Discover and advertise capabilities (Block 9).")
    {
        var discoverOpt = new Option<bool>("--discover", () => false, "Discover peer instances");
        var advertiseOpt = new Option<bool>("--advertise", () => false, "Advertise this instance's capabilities");
        var capabilityOpt = new Option<string?>("--capability", "Find peers with capability");

        var syncCmd = new Command("sync", "Pull shared adaptations from trusted peers and adopt after validation (P2.3)");
        syncCmd.SetHandler(async (InvocationContext ctx) => await ExecuteSyncAsync());

        var exportPathOpt = new Option<string>("--path", "Output path for export file") { IsRequired = true };
        var exportCmd = new Command("export", "Export shared adaptations to file for sneakernet transfer (P3.3)");
        exportCmd.AddOption(exportPathOpt);
        exportCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(exportPathOpt)!;
            await ExecuteExportAsync(path);
        });

        var importPathOpt = new Option<string>("--path", "Input path to export file") { IsRequired = true };
        var importCmd = new Command("import", "Import shared adaptations from sneakernet export file (P3.3)");
        importCmd.AddOption(importPathOpt);
        importCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForOption(importPathOpt)!;
            await ExecuteImportAsync(path);
        });

        AddOption(discoverOpt);
        AddOption(advertiseOpt);
        AddOption(capabilityOpt);
        AddCommand(syncCmd);
        AddCommand(exportCmd);
        AddCommand(importCmd);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var discover = ctx.ParseResult.GetValueForOption(discoverOpt);
            var advertise = ctx.ParseResult.GetValueForOption(advertiseOpt);
            var capability = ctx.ParseResult.GetValueForOption(capabilityOpt);
            await ExecuteAsync(discover, advertise, capability);
        });
    }

    private static async Task ExecuteSyncAsync()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure() // Uses repo root for adaptation db
            .AddSharedAdaptationCache()
            .BuildServiceProvider();

        var sync = services.GetRequiredService<ISharedAdaptationSync>();
        var entries = await sync.PullAsync().ConfigureAwait(false);
        Console.WriteLine($"Pulled {entries.Count} shared adaptation(s).");

        var adopted = 0;
        foreach (var entry in entries)
        {
            var ok = await sync.ValidateAndAdoptAsync(entry).ConfigureAwait(false);
            if (ok) adopted++;
        }
        Console.WriteLine($"Adopted {adopted}/{entries.Count}.");
        Environment.ExitCode = 0;
    }

    private static async Task ExecuteExportAsync(string outputPath)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure()
            .AddSharedAdaptationCache()
            .BuildServiceProvider();

        var transport = services.GetRequiredService<ISneakernetTransport>();
        await transport.ExportAsync(outputPath).ConfigureAwait(false);
        Console.WriteLine($"Exported to {outputPath}");
        Environment.ExitCode = 0;
    }

    private static async Task ExecuteImportAsync(string inputPath)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure()
            .AddSharedAdaptationCache()
            .BuildServiceProvider();

        var transport = services.GetRequiredService<ISneakernetTransport>();
        var adopted = await transport.ImportAsync(inputPath).ConfigureAwait(false);
        Console.WriteLine($"Imported {adopted} adaptation(s) from {inputPath}");
        Environment.ExitCode = 0;
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
