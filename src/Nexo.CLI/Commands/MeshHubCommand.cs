using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure;

namespace Nexo.CLI.Commands;

/// <summary>
/// Operator helpers for a shared Nexo.API hub and local instances.json (Phase 8).
/// </summary>
public sealed class MeshHubCommand : Command
{
    public MeshHubCommand() : base("hub", "Hub / fleet helpers: list local peers, probe remote /health.")
    {
        var listCmd = new Command("list", "List peers from instances.json (respects NEXO_MESH_TRUST_POLICY=allowlist admission filter)");
        listCmd.SetHandler(ExecuteList);

        var healthBaseOpt = new Option<string?>("--url", () => null, "Hub base URL, e.g. https://nexo.example:8080") { IsRequired = true };
        var healthTimeoutOpt = new Option<int>("--timeout-seconds", () => 30, "HTTP timeout");
        var healthCmd = new Command("health", "GET /health on a remote Nexo.API hub");
        healthCmd.Add(healthBaseOpt);
        healthCmd.Add(healthTimeoutOpt);
        healthCmd.SetHandler(ExecuteHealthAsync, healthBaseOpt, healthTimeoutOpt);

        AddCommand(listCmd);
        AddCommand(healthCmd);
    }

    private static void ExecuteList()
    {
        var instancesPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddMeshInfrastructure(instancesPath)
            .BuildServiceProvider();

        var discovery = services.GetRequiredService<IInstanceDiscovery>();
        var peers = discovery.DiscoverAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Peers ({peers.Count}):");
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
}
