using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>How a node serves its published packages to the LAN: the port to listen on, the
/// published-dir to serve (read-only), and the name it announces.</summary>
public sealed record MeshServeSettings(int Port, string PublishedDir, string NodeName);

/// <summary>
/// Shared wire rules for the federated mesh (F1): what a package file may be called, how large it may
/// be, and the shapes the peer endpoints exchange. Both the server (<see cref="MeshServeService"/>)
/// and the peer-pull client enforce these INDEPENDENTLY — a hostile or buggy peer on the network must
/// be contained by the client's own checks, never trusted to have applied the server's.
/// </summary>
public static class MeshWire
{
    /// <summary>Protocol tag, so a future incompatible change can be detected instead of guessed.</summary>
    public const string Version = "mesh/v1";

    /// <summary>
    /// Upper bound for one <c>.ashpkg</c> on the wire and on disk. Packages carry source text of a
    /// gated extension — legitimately tens of KB — so 4&#160;MB is generous headroom while still
    /// bounding what a peer can make this node download or buffer.
    /// </summary>
    public const long MaxPackageBytes = 4 * 1024 * 1024;

    private static readonly Regex SafeName =
        new(@"^[A-Za-z0-9][A-Za-z0-9._-]*\.ashpkg$", RegexOptions.Compiled);

    /// <summary>A bare, traversal-free package file name (what MeshStore.Publish produces).</summary>
    public static bool IsSafePackageName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && !name.Contains("..", StringComparison.Ordinal)
        && SafeName.IsMatch(name);

    /// <summary>One row of a peer's package index.</summary>
    public sealed record IndexEntry(string File, long Size);

    /// <summary>
    /// Reads at most <paramref name="maxBytes"/> from <paramref name="stream"/> and returns the text,
    /// or null when the stream exceeds the bound — the caller treats that as a hostile/oversized
    /// response, never a partial success.
    /// </summary>
    public static async Task<string?> ReadBoundedTextAsync(Stream stream, long maxBytes, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }
            if (ms.Length + read > maxBytes)
            {
                return null;
            }
            ms.Write(buffer, 0, read);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}

/// <summary>
/// F1 of the LAN party: a node SERVES its own published, signed <c>.ashpkg</c> to the network —
/// read-only, over three tiny endpoints — so peers can pull directly from it with no hub, no
/// director, and no shared folder:
/// <list type="bullet">
///   <item><c>GET /mesh/v1/hello</c> — who this node is: name, key fingerprint, package count.</item>
///   <item><c>GET /mesh/v1/index</c> — the published package list (file + size).</item>
///   <item><c>GET /mesh/v1/pkg/{file}</c> — one package's content.</item>
/// </list>
///
/// <para>Serving is safe by construction: everything offered is already sealed and Ed25519-signed
/// (MeshStore.Publish refuses to store what does not verify), the surface is read-only GET, file
/// names are validated against a traversal-free pattern AND resolved-path containment, and oversized
/// files are excluded. Trust still lives entirely on the RECEIVING side — a peer that pulls from here
/// runs every package through its own trust root and gate. The transport is not the trust.</para>
///
/// <para>Opt-in (registered only when <c>ASHLAR_MESH_SERVE_PORT</c> is set) and daemon-safe: a failed
/// bind or a request that throws is logged and never takes the node down. HttpListener keeps this
/// dependency-free — the same machinery the operator dashboard already uses.</para>
/// </summary>
public sealed class MeshServeService : BackgroundService
{
    private readonly MeshServeSettings _settings;
    private readonly ILogger<MeshServeService> _logger;

    /// <summary>Cap on in-flight requests, so a connection flood cannot exhaust the node.</summary>
    private const int MaxConcurrentRequests = 32;

    /// <summary>Per-request deadline — bounds the response write so a non-reading client frees its slot.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Creates the mesh serve service.</summary>
    public MeshServeService(MeshServeSettings settings, ILogger<MeshServeService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.Port is < 1 or > 65535)
        {
            _logger.LogInformation("Mesh serve disabled (no valid port).");
            return;
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://+:{_settings.Port}/");
        try
        {
            listener.Start();
        }
        catch (Exception ex) when (ex is HttpListenerException or PlatformNotSupportedException)
        {
            // A node that cannot serve is still a node — log loud, keep running everything else.
            _logger.LogError(ex, "Mesh serve could not bind port {Port} — serving disabled for this run.", _settings.Port);
            return;
        }

        _logger.LogInformation(
            "Mesh serve armed on :{Port} — offering {Dir} to the network (read-only, signed packages; trust is enforced by the puller).",
            _settings.Port, _settings.PublishedDir);

        // Requests are dispatched CONCURRENTLY and bounded: a slow or non-reading client can no
        // longer wedge the accept loop (each request runs off the loop), and the semaphore caps how
        // many can be in flight so a connection flood cannot exhaust threads. Excess is refused fast.
        using var slots = new SemaphoreSlim(MaxConcurrentRequests);
        await using var stopReg = stoppingToken.Register(() => { try { listener.Stop(); } catch { /* shutting down */ } });
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (stoppingToken.IsCancellationRequested)
                {
                    break;   // listener stopped by shutdown
                }
                catch (Exception ex)
                {
                    // A transient accept error must NOT escape ExecuteAsync — an unhandled throw here
                    // would fault the BackgroundService and (default StopHost) take the whole daemon
                    // down. Log, breathe, keep serving. HandleAsync's errors are already contained.
                    _logger.LogWarning(ex, "Mesh serve accept failed — continuing.");
                    try { await Task.Delay(200, stoppingToken).ConfigureAwait(false); } catch { break; }
                    continue;
                }

                if (!await slots.WaitAsync(0, stoppingToken).ConfigureAwait(false))
                {
                    TryClose(ctx, 503);   // at capacity — refuse fast rather than queue unboundedly
                    continue;
                }
                _ = DispatchAsync(ctx, slots, stoppingToken);
            }
        }
        finally
        {
            try { listener.Close(); } catch { /* best effort */ }
        }
    }

    private async Task DispatchAsync(HttpListenerContext ctx, SemaphoreSlim slots, CancellationToken stoppingToken)
    {
        // A per-request deadline bounds the response WRITE, so a client that stops draining the socket
        // frees its slot on the timeout instead of holding it forever.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(RequestTimeout);
        try
        {
            await HandleAsync(ctx, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Mesh serve request failed");
            }
            TryClose(ctx, 500);
        }
        finally
        {
            slots.Release();
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? string.Empty;
        if (!string.Equals(ctx.Request.HttpMethod, "GET", StringComparison.Ordinal))
        {
            TryClose(ctx, 405);
            return;
        }

        if (path is "/mesh/v1/hello")
        {
            string fingerprint;
            try { fingerprint = OperatorKey.TryLoad()?.Fingerprint ?? "(unsigned)"; }
            catch { fingerprint = "(unsigned)"; }
            await WriteJsonAsync(ctx, new
            {
                version = MeshWire.Version,
                name = _settings.NodeName,
                fingerprint,
                packages = ListPackages().Count,
            }, ct).ConfigureAwait(false);
            return;
        }

        if (path is "/mesh/v1/index")
        {
            await WriteJsonAsync(ctx, ListPackages(), ct).ConfigureAwait(false);
            return;
        }

        const string pkgPrefix = "/mesh/v1/pkg/";
        if (path.StartsWith(pkgPrefix, StringComparison.Ordinal))
        {
            var name = Uri.UnescapeDataString(path[pkgPrefix.Length..]);
            if (!MeshWire.IsSafePackageName(name))
            {
                TryClose(ctx, 404);
                return;
            }
            var full = Path.GetFullPath(Path.Combine(_settings.PublishedDir, name));
            // Containment on the RESOLVED path, independent of the name check.
            if (!string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(_settings.PublishedDir), StringComparison.Ordinal)
                || !File.Exists(full)
                || new FileInfo(full).Length > MeshWire.MaxPackageBytes)
            {
                TryClose(ctx, 404);
                return;
            }
            var bytes = await File.ReadAllBytesAsync(full, ct).ConfigureAwait(false);
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
            ctx.Response.Close();
            return;
        }

        TryClose(ctx, 404);
    }

    private List<MeshWire.IndexEntry> ListPackages()
    {
        if (!Directory.Exists(_settings.PublishedDir))
        {
            return [];
        }
        return Directory.EnumerateFiles(_settings.PublishedDir, "*.ashpkg")
            .Select(f => new FileInfo(f))
            .Where(fi => MeshWire.IsSafePackageName(fi.Name) && fi.Length <= MeshWire.MaxPackageBytes)
            .OrderBy(fi => fi.Name, StringComparer.Ordinal)
            .Select(fi => new MeshWire.IndexEntry(fi.Name, fi.Length))
            .ToList();
    }

    private static async Task WriteJsonAsync(HttpListenerContext ctx, object payload, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { WriteIndented = true });
        ctx.Response.ContentType = "application/json";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static void TryClose(HttpListenerContext ctx, int status)
    {
        try
        {
            ctx.Response.StatusCode = status;
            ctx.Response.Close();
        }
        catch { /* client gone */ }
    }
}
