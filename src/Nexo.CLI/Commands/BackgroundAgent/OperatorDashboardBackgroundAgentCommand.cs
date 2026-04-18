using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Localhost-only HTTP UI for operators: objectives / forge / observations snapshot
/// (same paths as <c>NEXO_*</c> env overrides and the background-agent CLI).
/// </summary>
public sealed class OperatorDashboardBackgroundAgentCommand
{
    private readonly ILogger<OperatorDashboardBackgroundAgentCommand> _logger;

    public OperatorDashboardBackgroundAgentCommand(ILogger<OperatorDashboardBackgroundAgentCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> RunAsync(int port, bool openBrowser, CancellationToken cancellationToken = default)
    {
        if (port is < 1 or > 65535)
        {
            Console.Error.WriteLine("Invalid --port (use 1-65535).");
            return 2;
        }

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
        Console.WriteLine("Press Ctrl+C to stop.");

        if (openBrowser)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, cts.Token).ConfigureAwait(false);
                    Process.Start(new ProcessStartInfo { FileName = prefix, UseShellExecute = true });
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

    private static async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        try
        {
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
    pre { background: #161b22; padding: 1rem; overflow: auto; border-radius: 8px; font-size: 12px; }
    .muted { color: #8b949e; font-size: 0.85rem; }
    button { margin-top: 0.5rem; padding: 0.4rem 0.8rem; cursor: pointer; border-radius: 6px; border: 1px solid #30363d; background: #21262d; color: #e6edf3; }
  </style>
</head>
<body>
  <h1>Runtime Studio — operator dashboard</h1>
  <p class="muted">Read-only view of local paths (same as <code>NEXO_*</code> env). Binds to 127.0.0.1 only.</p>
  <button type="button" id="refresh">Refresh</button>
  <pre id="out">Loading…</pre>
  <script>
    async function load() {
      const el = document.getElementById('out');
      try {
        const r = await fetch('/api/summary.json', { cache: 'no-store' });
        const j = await r.json();
        el.textContent = JSON.stringify(j, null, 2);
      } catch (e) {
        el.textContent = String(e);
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
