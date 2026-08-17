using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Nexo.Tests.Infrastructure.Helpers.Ncr;

/// <summary>
/// Minimal loopback HTTP server matching the paths used by production <see cref="Nexo.Infrastructure.Execution.Routing.RunPodHttpClient"/>.
/// Lets integration tests exercise the real HTTP client without calling RunPod cloud APIs.
/// </summary>
public sealed class RunPodLoopbackApiServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public RunPodLoopbackApiServer(RunPodLoopbackApiConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public RunPodLoopbackApiConfiguration Configuration { get; }

    public string BaseUrl { get; private set; } = "";

    public void Start()
    {
        var port = GetFreeTcpPort();
        var prefix = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        BaseUrl = prefix.TrimEnd('/') + "/";
        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = HandleAsync(ctx);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var path = req.Url?.AbsolutePath ?? "";
            var method = req.HttpMethod;

            if (method == "POST" && path.Equals("/v2/instances", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(
                    ctx,
                    200,
                    JsonSerializer.SerializeToUtf8Bytes(new { instanceId = Configuration.InstanceId, id = Configuration.InstanceId }));
                return;
            }

            if (method == "POST" && path.StartsWith("/v2/instances/", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith("/jobs", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(
                    ctx,
                    200,
                    JsonSerializer.SerializeToUtf8Bytes(new { jobId = Configuration.JobId, id = Configuration.JobId }));
                return;
            }

            if (method == "GET" && path.StartsWith("/v2/jobs/", StringComparison.OrdinalIgnoreCase))
            {
                if (path.EndsWith("/result", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/octet-stream";
                    await ctx.Response.OutputStream.WriteAsync(Configuration.PullBytes).ConfigureAwait(false);
                    ctx.Response.Close();
                    return;
                }

                var status = Configuration.PollStatuses.Count > 0
                    ? Configuration.PollStatuses.Dequeue()
                    : new RunPodLoopbackPollStatus { status = "completed", message = "done" };
                await WriteJsonAsync(ctx, 200, JsonSerializer.SerializeToUtf8Bytes(status)).ConfigureAwait(false);
                return;
            }

            if (method == "DELETE" && path.StartsWith("/v2/instances/", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
                return;
            }

            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }
        catch
        {
            try
            {
                ctx.Response.Abort();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task WriteJsonAsync(HttpListenerContext ctx, int code, byte[] utf8Json)
    {
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.OutputStream.WriteAsync(utf8Json).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static int GetFreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
        }
        catch
        {
            // ignore
        }

        try
        {
            _listener.Stop();
        }
        catch
        {
            // ignore
        }

        // Teardown must never fail a test that already passed. On macOS/Linux the managed
        // HttpListener re-binds the prefix's endpoint while tearing it down and can throw
        // HttpListenerException "Address already in use" from Close() when the just-released
        // ephemeral port is still in TIME_WAIT (readiness run 31982502428).
        try
        {
            _listener.Close();
        }
        catch (HttpListenerException)
        {
            // ignore
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        _cts.Dispose();
        try
        {
            _loop?.GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }
    }

}
