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

        _listener.Close();
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

/// <summary>Configurable responses for <see cref="RunPodLoopbackApiServer"/>.</summary>
public sealed class RunPodLoopbackApiConfiguration
{
    public string InstanceId { get; set; } = "loopback-inst";

    public string JobId { get; set; } = "loopback-job";

    public byte[] PullBytes { get; set; } = [1];

    public Queue<RunPodLoopbackPollStatus> PollStatuses { get; } = new();
}

/// <summary>Serialized JSON shape for job poll responses.</summary>
public sealed class RunPodLoopbackPollStatus
{
    public string status { get; set; } = "completed";
    public string? message { get; set; }
}
