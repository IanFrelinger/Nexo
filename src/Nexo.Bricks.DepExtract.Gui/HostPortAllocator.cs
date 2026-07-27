using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Nexo.Bricks.DepExtract.Gui;

/// <summary>
/// Finds free host ports for parser GUI + extract agent when defaults collide.
/// Combines Docker published-port inventory with TCP probes (host.docker.internal / 127.0.0.1).
/// </summary>
public static class HostPortAllocator
{
    // Host publish side only, e.g. 0.0.0.0:18080->8080/tcp (ignore container 8080).
    static readonly Regex DockerHostPort = new(
        @":(\d{2,5})->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task<PortSuggestion> SuggestAsync(
        int preferParser = PortSettingsStore.DefaultParserHostPort,
        int preferAgent = PortSettingsStore.DefaultAgentHostPort,
        CancellationToken ct = default)
    {
        if (preferParser is < 1 or > 65535) preferParser = PortSettingsStore.DefaultParserHostPort;
        if (preferAgent is < 1 or > 65535) preferAgent = PortSettingsStore.DefaultAgentHostPort;

        var busy = await CollectBusyPortsAsync(ct);
        var reasons = new List<string>();

        var parser = await FindFreeAsync(preferParser, busy, exclude: null, reasons, "parser", ct);
        busy.Add(parser);
        var agent = await FindFreeAsync(preferAgent, busy, exclude: parser, reasons, "agent", ct);

        var changed = parser != preferParser || agent != preferAgent;
        var summary = changed
            ? $"Selected free ports parser={parser}, agent={agent}" +
              (reasons.Count > 0 ? $" ({string.Join("; ", reasons.Take(4))})" : "")
            : $"Preferred ports are free (parser={parser}, agent={agent})";

        return new PortSuggestion(parser, agent, preferParser, preferAgent, changed, summary, busy.OrderBy(x => x).ToArray());
    }

    static async Task<int> FindFreeAsync(
        int prefer,
        HashSet<int> busy,
        int? exclude,
        List<string> reasons,
        string label,
        CancellationToken ct)
    {
        if (prefer != exclude && !busy.Contains(prefer) && await IsLikelyFreeAsync(prefer, ct))
            return prefer;

        if (busy.Contains(prefer) || prefer == exclude)
            reasons.Add($"{label} preferred {prefer} busy/reserved");
        else
            reasons.Add($"{label} preferred {prefer} probe failed");

        // Scan upward from prefer, then a high ephemeral band, then from 1024.
        foreach (var candidate in Candidates(prefer))
        {
            ct.ThrowIfCancellationRequested();
            if (candidate == exclude || busy.Contains(candidate)) continue;
            if (await IsLikelyFreeAsync(candidate, ct))
                return candidate;
            busy.Add(candidate);
        }

        throw new InvalidOperationException($"No free host port found for {label} near {prefer}");
    }

    static IEnumerable<int> Candidates(int prefer)
    {
        for (var p = prefer + 1; p <= Math.Min(prefer + 200, 65535); p++)
            yield return p;
        for (var p = 28080; p <= 28280; p++)
            yield return p;
        for (var p = 15237; p <= 15437; p++)
            yield return p;
        for (var p = 1024; p <= 2048; p++)
            yield return p;
    }

    static async Task<HashSet<int>> CollectBusyPortsAsync(CancellationToken ct)
    {
        var busy = new HashSet<int>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            // Published ports look like: 0.0.0.0:18080->8080/tcp, [::]:5237->5237/tcp
            psi.ArgumentList.Add("ps");
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add("{{.Ports}}");
            using var p = Process.Start(psi);
            if (p is null) return busy;
            var stdout = await p.StandardOutput.ReadToEndAsync(ct);
            _ = await p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0) return busy;

            foreach (Match m in DockerHostPort.Matches(stdout))
            {
                if (int.TryParse(m.Groups[1].Value, out var port) && port is >= 1 and <= 65535)
                    busy.Add(port);
            }
        }
        catch
        {
            /* docker optional for probing */
        }

        return busy;
    }

    /// <summary>
    /// Port is free if we cannot connect (nothing listening) and, when possible, can bind locally.
    /// </summary>
    public static async Task<bool> IsLikelyFreeAsync(int port, CancellationToken ct)
    {
        if (port is < 1 or > 65535) return false;

        foreach (var host in ProbeHosts())
        {
            try
            {
                using var client = new TcpClient();
                using var reg = ct.Register(() => { try { client.Dispose(); } catch { /* ignore */ } });
                var connect = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connect, Task.Delay(200, ct));
                if (completed == connect && client.Connected)
                    return false; // something accepted → in use
            }
            catch (SocketException)
            {
                // connection refused / unreachable → treat as free for this host
            }
            catch (ObjectDisposedException)
            {
                // cancelled probe
            }
            catch
            {
                /* ignore */
            }
        }

        // Extra check when running on the host network namespace (dotnet run / --network host).
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch
        {
            return true;
        }
    }

    static IEnumerable<string> ProbeHosts()
    {
        yield return "127.0.0.1";
        var overrideHost = Environment.GetEnvironmentVariable("NEXO_PORT_PROBE_HOST");
        if (!string.IsNullOrWhiteSpace(overrideHost))
            yield return overrideHost.Trim();
        // Docker Desktop / typical Linux gateway alias for host services
        yield return "host.docker.internal";
    }
}

public sealed record PortSuggestion(
    int ParserHostPort,
    int AgentHostPort,
    int PreferredParserHostPort,
    int PreferredAgentHostPort,
    bool ChangedFromPreferred,
    string Summary,
    IReadOnlyList<int> BusySample);
