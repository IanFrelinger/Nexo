using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Mesh;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Mesh;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 9: Instance discovery and capability mesh.
/// P2.3: nexo mesh sync pulls and validates shared adaptations before adoption.
/// </summary>
public sealed class MeshCommand : Command
{
    /// <summary>Creates a new MeshCommand instance.</summary>
    public MeshCommand() : base("mesh", "Discover and advertise capabilities (Block 9).")
    {
        var discoverOpt = new Option<bool>("--discover", () => false, "Discover peer instances");
        var advertiseOpt = new Option<bool>("--advertise", () => false, "Advertise this instance's capabilities");
        var capabilityOpt = new Option<string?>("--capability", "Find peers with capability");
        var setTrustTierOpt = new Option<string?>("--set-trust-tier", "Update peer trust tier using <peerId>:<trusted|untrusted|unknown> in instances.json");

        var syncCmd = new Command("sync", "Pull shared adaptations from trusted peers and adopt after validation (P2.3)");
        syncCmd.SetHandler(async (InvocationContext ctx) => await ExecuteSyncAsync());

        var capabilitiesCmd = new Command("capabilities", "Show local instance capabilities for mesh negotiation");
        capabilitiesCmd.SetHandler(ExecuteCapabilities);

        var exportComponentArg = new Argument<string?>("componentId", () => null, "Component/adaptation ID to export; omit to export all");
        var exportToOpt = new Option<string>("--to", "Output path for .nxpkg export file") { IsRequired = true };
        var exportCmd = new Command("export", "Export shared adaptations to .nxpkg file for sneakernet transfer (P3.3)");
        exportCmd.AddArgument(exportComponentArg);
        exportCmd.AddOption(exportToOpt);
        exportCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var componentId = ctx.ParseResult.GetValueForArgument(exportComponentArg);
            var to = ctx.ParseResult.GetValueForOption(exportToOpt)!;
            await ExecuteExportAsync(to, componentId);
        });

        var importPathArg = new Argument<string>("path", "Path to .nxpkg import file");
        var importCmd = new Command("import", "Import shared adaptations from .nxpkg sneakernet file (P3.3)");
        importCmd.AddArgument(importPathArg);
        importCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var path = ctx.ParseResult.GetValueForArgument(importPathArg)!;
            await ExecuteImportAsync(path);
        });

        var peersCmd = new Command("peers", "List peers from local instances.json (open mesh primitive; not fleet director nodes)");
        peersCmd.SetHandler(ExecutePeers);

        var healthUrlOpt = new Option<string?>("--url", () => null, "Remote host base URL, e.g. https://nexo.example:8080") { IsRequired = true };
        var healthTimeoutOpt = new Option<int>("--timeout-seconds", () => 30, "HTTP timeout");
        var healthCmd = new Command("health", "GET /health on a remote Nexo host (generic probe)");
        healthCmd.Add(healthUrlOpt);
        healthCmd.Add(healthTimeoutOpt);
        healthCmd.SetHandler(ExecuteHealthAsync, healthUrlOpt, healthTimeoutOpt);

        var admitPeerArg = new Argument<string>("peerId", "Peer id to admit in local instances.json");
        var admitCmd = new Command("admit", "Set admitted=true in local instances.json (not fleet director; use commercial mesh director admit for /api/mesh/fleet)");
        admitCmd.AddArgument(admitPeerArg);
        admitCmd.SetHandler((InvocationContext ctx) =>
        {
            var peerId = ctx.ParseResult.GetValueForArgument(admitPeerArg)!;
            var ok = UpdatePeerAdmitted(peerId, admitted: true);
            if (!ok)
            {
                Console.Error.WriteLine($"Peer '{peerId}' not found in instances.json.");
                ctx.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Admitted peer '{peerId}' (admitted=true).");
            ctx.ExitCode = 0;
        });

        var revokePeerArg = new Argument<string>("peerId", "Peer id to revoke in local instances.json");
        var revokeCmd = new Command("revoke", "Set admitted=false in local instances.json (not fleet director; use commercial mesh director revoke for /api/mesh/fleet)");
        revokeCmd.AddArgument(revokePeerArg);
        revokeCmd.SetHandler((InvocationContext ctx) =>
        {
            var peerId = ctx.ParseResult.GetValueForArgument(revokePeerArg)!;
            var ok = UpdatePeerAdmitted(peerId, admitted: false);
            if (!ok)
            {
                Console.Error.WriteLine($"Peer '{peerId}' not found in instances.json.");
                ctx.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Revoked peer '{peerId}' (admitted=false).");
            ctx.ExitCode = 0;
        });

        AddOption(discoverOpt);
        AddOption(advertiseOpt);
        AddOption(capabilityOpt);
        AddOption(setTrustTierOpt);
        AddCommand(syncCmd);
        AddCommand(capabilitiesCmd);
        AddCommand(exportCmd);
        AddCommand(importCmd);
        AddCommand(peersCmd);
        AddCommand(healthCmd);
        AddCommand(admitCmd);
        AddCommand(revokeCmd);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var discover = ctx.ParseResult.GetValueForOption(discoverOpt);
            var advertise = ctx.ParseResult.GetValueForOption(advertiseOpt);
            var capability = ctx.ParseResult.GetValueForOption(capabilityOpt);
            var setTrustTier = ctx.ParseResult.GetValueForOption(setTrustTierOpt);
            await ExecuteAsync(discover, advertise, capability, setTrustTier);
        });
    }

    private static void ExecuteCapabilities()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMeshInfrastructure()
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IInstanceCapabilitiesProvider>();
        var caps = provider.GetCapabilities();

        Console.WriteLine("Local instance capabilities:");
        Console.WriteLine($"  Supported formats: {string.Join(", ", caps.SupportedFormats)}");
        Console.WriteLine($"  Preferred format: {caps.PreferredFormat}");
        Console.WriteLine($"  CanCompile: {caps.CanCompile}");
        Console.WriteLine($"  HasDockerRuntime: {caps.HasDockerRuntime}");
        Console.WriteLine($"  HasWasmRuntime: {caps.HasWasmRuntime}");
        Console.WriteLine($"  IsAirGapped: {caps.IsAirGapped}");
        Console.WriteLine($"  Available components: {string.Join(", ", caps.AvailableComponents)}");
        Environment.ExitCode = 0;
    }

    private static async Task ExecuteSyncAsync()
    {
        var sharedPath = Environment.GetEnvironmentVariable("NEXO_SHARED_ADAPTATIONS_PATH");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure() // Uses repo root for adaptation db
            .AddSharedAdaptationCache(sharedPath)
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

    private static async Task ExecuteExportAsync(string outputPath, string? componentId = null)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure()
            .AddSharedAdaptationCache()
            .BuildServiceProvider();

        var transport = services.GetRequiredService<ISneakernetTransport>();
        await transport.ExportAsync(outputPath, componentId).ConfigureAwait(false);
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

    private static async Task ExecuteAsync(bool discover, bool advertise, string? capability, string? setTrustTier)
    {
        if (!string.IsNullOrWhiteSpace(setTrustTier))
        {
            if (!TryParseTrustTierChange(setTrustTier, out var peerId, out var trustTier))
            {
                Console.Error.WriteLine("Invalid --set-trust-tier value. Use <peerId>:<trusted|untrusted|unknown>.");
                Environment.ExitCode = 1;
                return;
            }

            var updated = UpdatePeerTrustTier(peerId!, trustTier);
            if (!updated)
            {
                Console.Error.WriteLine($"Peer '{peerId}' not found in instances.json.");
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Updated trust tier for peer '{peerId}' => {trustTier}.");
        }

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMeshInfrastructure()
            .BuildServiceProvider();

        if (discover)
        {
            var discovery = services.GetRequiredService<IInstanceDiscovery>();
            var peers = await discovery.DiscoverAsync().ConfigureAwait(false);
            var trustPolicy = MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy();
            Console.WriteLine($"Active mesh trust policy: {trustPolicy} (NEXO_MESH_TRUST_POLICY or NEXO_PEER_TRUST_POLICY; default any)");
            Console.WriteLine($"Discovered {peers.Count} peer(s):");
            foreach (var p in peers)
                Console.WriteLine($"  - {p.PeerId} @ {p.Endpoint} tier={p.TrustTier} [{string.Join(", ", p.Capabilities)}]");
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

        if (!discover && !advertise && string.IsNullOrEmpty(capability) && string.IsNullOrWhiteSpace(setTrustTier))
            Console.WriteLine(
                "Use --discover, --advertise, --capability <name>, --set-trust-tier <peerId>:<tier>, " +
                "mesh peers, mesh health, mesh admit, or mesh revoke (local instances.json). " +
                "Fleet director HTTP (register, admit, list-nodes) lives in commercial/src/Nexo.Commercial.MeshDirector.");
        Environment.ExitCode = 0;
    }

    private static void ExecutePeers()
    {
        var instancesPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMeshInfrastructure(instancesPath)
            .BuildServiceProvider();

        var discovery = services.GetRequiredService<IInstanceDiscovery>();
        var peers = discovery.DiscoverAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Local peers from instances.json ({peers.Count}):");
        foreach (var p in peers)
        {
            Console.WriteLine(
                $"  {p.PeerId,-24} {p.Endpoint,-40} tier={p.TrustTier,-10} admitted={p.Admitted}");
        }

        Environment.ExitCode = 0;
    }

    private static async Task ExecuteHealthAsync(string? baseUrl, int timeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.Error.WriteLine("--url is required.");
            Environment.ExitCode = 1;
            return;
        }

        var root = baseUrl.Trim().TrimEnd('/');
        var uri = new Uri(root + "/health", UriKind.Absolute);
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120)) };
        try
        {
            using var resp = await client.GetAsync(uri).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            if (!string.IsNullOrWhiteSpace(body))
                Console.WriteLine(body);
            Environment.ExitCode = resp.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private static bool TryParseTrustTierChange(string value, out string? peerId, out PeerTrustTier tier)
    {
        peerId = null;
        tier = PeerTrustTier.Unknown;
        var split = value.Split(':', 2, StringSplitOptions.TrimEntries);
        if (split.Length != 2 || string.IsNullOrWhiteSpace(split[0]))
            return false;
        if (!Enum.TryParse<PeerTrustTier>(split[1], true, out tier))
            return false;
        peerId = split[0];
        return true;
    }

    private static bool UpdatePeerTrustTier(string peerId, PeerTrustTier tier)
    {
        return UpdatePeerJsonField(peerId, obj => obj["trustTier"] = tier.ToString());
    }

    private static bool UpdatePeerAdmitted(string peerId, bool admitted)
    {
        return UpdatePeerJsonField(peerId, obj => obj["admitted"] = admitted);
    }

    private static bool UpdatePeerJsonField(string peerId, Action<JsonObject> mutator)
    {
        var instancesPath = ResolveInstancesPath();
        if (!File.Exists(instancesPath))
            return false;

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(instancesPath)) as JsonArray;
            if (root == null)
                return false;

            var entry = root
                .OfType<JsonObject>()
                .FirstOrDefault(obj => string.Equals(obj["peerId"]?.GetValue<string>(), peerId, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return false;

            mutator(entry);
            File.WriteAllText(instancesPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveInstancesPath()
    {
        var configured = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured.Trim());
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
    }

}
