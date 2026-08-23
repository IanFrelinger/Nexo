using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// Localhost-only HTTP UI for operators: objectives / forge / observations snapshot
/// (same paths as <c>ASHLAR_*</c> env overrides and the background-agent CLI).
/// </summary>
public sealed class OperatorDashboardBackgroundAgentCommand
{
    private readonly ILogger<OperatorDashboardBackgroundAgentCommand> _logger;
    private string? _expectedAuthToken;

    /// <summary>Creates a new OperatorDashboardBackgroundAgentCommand instance.</summary>
    public OperatorDashboardBackgroundAgentCommand(ILogger<OperatorDashboardBackgroundAgentCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Creates a new RunAsync instance.</summary>
    public async Task<int> RunAsync(int port, bool openBrowser, string? authToken, CancellationToken cancellationToken = default)
    {
        if (port is < 1 or > 65535)
        {
            Console.Error.WriteLine("Invalid --port (use 1-65535).");
            return 2;
        }

        var fromEnv = Environment.GetEnvironmentVariable("ASHLAR_DASHBOARD_AUTH_TOKEN");
        _expectedAuthToken = string.IsNullOrWhiteSpace(authToken)
            ? (string.IsNullOrWhiteSpace(fromEnv) ? null : fromEnv.Trim())
            : authToken.Trim();
        if (_expectedAuthToken is { Length: 0 })
            _expectedAuthToken = null;

        var prefix = $"http://127.0.0.1:{port}/";
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            _logger.LogError(ex, "Failed to bind dashboard listener on {Prefix}", prefix);
            Console.Error.WriteLine($"Could not listen on {prefix} ({ex.Message}). Try another --port.");
            return 1;
        }

        Console.CancelKeyPress += (_, a) =>
        {
            a.Cancel = true;
            try { listener.Stop(); } catch { /* ignore */ }
            cts.Cancel();
        };

        Console.WriteLine($"Runtime Studio operator dashboard: {prefix}");
        if (_expectedAuthToken is not null)
            Console.WriteLine("Auth is enabled: pass ?token=... in the URL or Authorization: Bearer <token>.");
        Console.WriteLine("Press Ctrl+C to stop.");

        if (openBrowser)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, cts.Token).ConfigureAwait(false);
                    var url = prefix + (_expectedAuthToken is null ? "" : "?token=" + Uri.EscapeDataString(_expectedAuthToken));
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not open browser");
                }
            }, CancellationToken.None);
        }

        try
        {
            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(ctx), CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            /* ctrl+c */
        }
        finally
        {
            try { listener.Stop(); } catch { /* ignore */ }
        }

        return 0;
    }

    private bool IsAuthorized(HttpListenerRequest req)
    {
        if (_expectedAuthToken is null)
            return true;

        var q = req.QueryString["token"];
        if (!string.IsNullOrEmpty(q) && string.Equals(q, _expectedAuthToken, StringComparison.Ordinal))
            return true;

        var auth = req.Headers["Authorization"];
        if (!string.IsNullOrEmpty(auth) &&
            auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var t = auth["Bearer ".Length..].Trim();
            if (string.Equals(t, _expectedAuthToken, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        try
        {
            if (!IsAuthorized(ctx.Request))
            {
                ctx.Response.StatusCode = 401;
                var msg = Encoding.UTF8.GetBytes("Unauthorized");
                ctx.Response.ContentLength64 = msg.Length;
                await ctx.Response.OutputStream.WriteAsync(msg).ConfigureAwait(false);
                return;
            }

            if (string.Equals(path, "/api/summary.json", StringComparison.OrdinalIgnoreCase))
            {
                var json = RuntimeStudioOperatorDashboardSummary.BuildJson(
                    RuntimeStudioOperatorDashboardSummary.ResolvePaths());
                var buf = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = buf.Length;
                await ctx.Response.OutputStream.WriteAsync(buf).ConfigureAwait(false);
                return;
            }

            if (path is "/" or "/index.html")
            {
                var html = Encoding.UTF8.GetBytes(DashboardHtml);
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength64 = html.Length;
                await ctx.Response.OutputStream.WriteAsync(html).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 404;
        }
        catch
        {
            try { ctx.Response.StatusCode = 500; } catch { /* ignore */ }
        }
        finally
        {
            try { ctx.Response.OutputStream.Close(); } catch { /* ignore */ }
        }
    }

    private const string DashboardHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Runtime Studio — operator</title>
  <style>
    :root { font-family: system-ui, sans-serif; background: #0f1419; color: #e6edf3; }
    body { max-width: 960px; margin: 2rem auto; padding: 0 1rem; }
    h1 { font-size: 1.25rem; }
    h2 { font-size: 1rem; margin-top: 1.25rem; }
    pre { background: #161b22; padding: 1rem; overflow: auto; border-radius: 8px; font-size: 12px; }
    .muted { color: #8b949e; font-size: 0.85rem; }
    .cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 0.5rem; margin: 0.75rem 0; }
    .card { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 0.6rem 0.75rem; }
    .card b { display: block; font-size: 1.1rem; }
    .card span { font-size: 0.75rem; color: #8b949e; }
    button { margin-top: 0.5rem; margin-right: 0.5rem; padding: 0.4rem 0.8rem; cursor: pointer; border-radius: 6px; border: 1px solid #30363d; background: #21262d; color: #e6edf3; }
    #summary { white-space: pre-wrap; background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 0.75rem 1rem; font-size: 0.9rem; line-height: 1.45; }
  </style>
</head>
<body>
  <h1>Runtime Studio — operator dashboard</h1>
  <p class="muted">Read-only view of local paths (same as <code>ASHLAR_*</code> env). Binds to 127.0.0.1 only. With auth, use <code>?token=</code> once or set the Authorization header.</p>
  <button type="button" id="refresh">Refresh</button>
  <h2>At a glance</h2>
  <div class="cards" id="cards"></div>
  <div id="summary">Loading…</div>
  <h2>Raw JSON</h2>
  <details>
    <summary style="cursor:pointer;color:#8b949e;">Expand</summary>
    <pre id="out"></pre>
  </details>
  <script>
    function pick(obj, a, b) { return (obj && obj[a] !== undefined) ? obj[a] : (obj && obj[b]); }
    function fmt(n) { return n === null || n === undefined ? '—' : (typeof n === 'number' ? n.toFixed(1) : String(n)); }
    function renderCards(j) {
      const m = pick(j, 'metrics', 'Metrics');
      const el = document.getElementById('cards');
      el.innerHTML = '';
      if (!m) return;
      const obs = pick(m, 'observationsFileBytes', 'ObservationsFileBytes');
      const by = pick(m, 'objectivesByStatus', 'ObjectivesByStatus') || {};
      const pr = pick(m, 'proposalsByStatus', 'ProposalsByStatus') || {};
      const sla = pick(m, 'objectiveSla', 'ObjectiveSla') || {};
      const add = (label, val, sub) => {
        const d = document.createElement('div');
        d.className = 'card';
        d.innerHTML = '<b>' + val + '</b><span>' + label + '</span>' + (sub ? '<div style="font-size:0.7rem;margin-top:0.25rem;color:#6e7681;">' + sub + '</div>' : '');
        el.appendChild(d);
      };
      add('Pending', by.Pending ?? by.pending ?? 0);
      add('In progress', by.InProgress ?? by.inProgress ?? 0);
      add('Blocked', by.Blocked ?? by.blocked ?? 0);
      add('Proposals (proposed)', pr.Proposed ?? pr.proposed ?? 0);
      add('Obs log (bytes)', obs === null || obs === undefined ? '—' : obs, '');
      const lastTs = pick(m, 'observationsLastTimestamp', 'ObservationsLastTimestamp');
      if (lastTs != null && lastTs !== undefined)
        add('Last obs (UTC)', String(lastTs).slice(0, 24), '');
      const oph = pick(sla, 'oldestPendingAgeHours', 'OldestPendingAgeHours');
      const oih = pick(sla, 'oldestInProgressAgeHours', 'OldestInProgressAgeHours');
      if (oph != null) add('Oldest pending (h)', fmt(oph), '');
      if (oih != null) add('Oldest in-prog (h)', fmt(oih), '');
    }
    function renderSummary(j) {
      const el = document.getElementById('summary');
      const mode = pick(j, 'mode', 'Mode') || '—';
      const paths = pick(j, 'paths', 'Paths') || {};
      const lines = ['Mode: ' + mode, 'Objectives: ' + (paths.objectivesRoot || paths.ObjectivesRoot || '—'), 'Forge: ' + (paths.forgeRoot || paths.ForgeRoot || '—'), 'Observations: ' + (paths.observationsPath || paths.ObservationsPath || '—')];
      el.textContent = lines.join('\n');
    }
    async function load() {
      const raw = document.getElementById('out');
      const sum = document.getElementById('summary');
      try {
        const q = new URLSearchParams(location.search);
        const tok = q.get('token');
        const headers = {};
        if (tok) headers['Authorization'] = 'Bearer ' + tok;
        const r = await fetch('/api/summary.json', { cache: 'no-store', headers });
        if (r.status === 401) {
          sum.textContent = '401 Unauthorized — add ?token=YOUR_TOKEN to the URL if auth is enabled.';
          raw.textContent = '';
          return;
        }
        const j = await r.json();
        renderCards(j);
        renderSummary(j);
        raw.textContent = JSON.stringify(j, null, 2);
      } catch (e) {
        sum.textContent = String(e);
        raw.textContent = '';
      }
    }
    document.getElementById('refresh').onclick = load;
    load();
    setInterval(load, 15000);
  </script>
</body>
</html>
""";
}
