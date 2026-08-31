using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>How a node serves its published packages: the port, the read-only published dir, the name
/// it announces, and optional TLS/mTLS for a private fleet (a PEM server cert, and — for mutual TLS —
/// a requirement that clients present a cert the fleet CA signed).</summary>
public sealed record MeshServeSettings(
    int Port, string PublishedDir, string NodeName,
    string? TlsCertPath = null, string? TlsKeyPath = null, bool RequireClientCert = false, string? CaPath = null)
{
    /// <summary>True when a server certificate is configured (endpoint is HTTPS).</summary>
    public bool Tls => !string.IsNullOrWhiteSpace(TlsCertPath) && !string.IsNullOrWhiteSpace(TlsKeyPath);
}

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

    /// <summary>Shared JSON options for the index round-trip. Case-insensitive so the client is robust
    /// to either casing on the wire — Kestrel's <c>Results.Json</c> emits camelCase by default.</summary>
    public static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

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
/// <para>Built on Kestrel, so the same endpoint serves plaintext HTTP on the LAN or TLS/mTLS for a
/// private fleet (a PEM server cert; optionally requiring a client cert the fleet CA signed) — and
/// Kestrel's connection limits, header timeout, and minimum-response-data-rate give the DoS
/// protections a raw socket loop had to hand-roll: a slow or non-reading client is dropped, not able
/// to wedge serving.</para>
///
/// <para>Safe by construction: everything offered is already sealed and Ed25519-signed (MeshStore
/// refuses to store what does not verify), the surface is read-only GET, file names are validated
/// against a traversal-free pattern AND resolved-path containment, oversized files are excluded, and a
/// failed bind is logged and never takes the daemon down. Trust still lives entirely on the RECEIVING
/// side — a peer that pulls from here runs every package through its own trust root and gate; TLS is
/// access control and confidentiality on top, not a substitute for the seal.</para>
///
/// <para>Opt-in (registered only when <c>ASHLAR_MESH_SERVE_PORT</c> is set).</para>
/// </summary>
public sealed class MeshServeService : BackgroundService
{
    private readonly MeshServeSettings _settings;
    private readonly ILogger<MeshServeService> _logger;

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

        // Fail CLOSED on a half-configured private-fleet TLS setup. A node asked to require client
        // certs — or given only one of cert/key — must NEVER silently fall back to plaintext, which
        // would expose the package index/files to any client with no cert. Refuse to serve instead.
        var configError = ConfigError(_settings);
        if (configError is not null)
        {
            _logger.LogError(
                "Mesh serve refusing to start on :{Port} — {Error} Not serving (fail-closed; no plaintext fallback).",
                _settings.Port, configError);
            return;
        }

        WebApplication app;
        try
        {
            app = BuildApp();
            await app.StartAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A node that cannot serve is still a node — log loud, keep everything else running. A
            // mis-configured cert (unreadable PEM, wrong key) surfaces HERE, not as silent plaintext.
            _logger.LogError(ex, "Mesh serve could not start on :{Port} — serving disabled for this run.", _settings.Port);
            return;
        }

        _logger.LogInformation(
            "Mesh serve armed on :{Port} ({Scheme}) — offering {Dir} to the network (read-only, signed packages; trust is enforced by the puller).",
            _settings.Port, _settings.Tls ? (_settings.RequireClientCert ? "mTLS" : "TLS") : "http", _settings.PublishedDir);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            try { await app.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); } catch { /* best effort */ }
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns a message when the TLS/mTLS settings are half-specified in a way that must NOT degrade
    /// to plaintext, or null when the config is coherent (including the intended no-TLS LAN default:
    /// cert, key, require, and CA all unset). Pure, so the fail-closed rules are directly testable.
    /// </summary>
    public static string? ConfigError(MeshServeSettings s)
    {
        var certSet = !string.IsNullOrWhiteSpace(s.TlsCertPath);
        var keySet = !string.IsNullOrWhiteSpace(s.TlsKeyPath);
        if (certSet != keySet)
        {
            return "TLS needs BOTH a cert and a key (ASHLAR_MESH_SERVE_TLS_CERT + _TLS_KEY).";
        }
        if (s.RequireClientCert && !s.Tls)
        {
            return "client-cert (mTLS) was required (ASHLAR_MESH_SERVE_REQUIRE_CLIENT_CERT=1) but no server cert/key is set.";
        }
        if (s.RequireClientCert && string.IsNullOrWhiteSpace(s.CaPath))
        {
            return "client-cert (mTLS) was required but no CA (ASHLAR_MESH_SERVE_CA) is set to validate client certs against.";
        }
        return null;
    }

    private WebApplication BuildApp()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();   // the daemon owns logging; don't double up
        builder.WebHost.ConfigureKestrel(k =>
        {
            k.AddServerHeader = false;
            k.Limits.MaxConcurrentConnections = 100;
            k.Limits.MaxRequestBodySize = 0;                          // GET only
            k.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
            k.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
            // Kestrel aborts a connection that reads slower than its default MinResponseDataRate —
            // this is the built-in defence against the slow/non-reading client that a raw HttpListener
            // loop had to guard by hand.
            k.ListenAnyIP(_settings.Port, listen =>
            {
                if (_settings.Tls)
                {
                    var serverCert = MeshTls.LoadCertWithKey(_settings.TlsCertPath!, _settings.TlsKeyPath!);
                    listen.UseHttps(https =>
                    {
                        https.ServerCertificate = serverCert;
                        if (_settings.RequireClientCert)
                        {
                            if (string.IsNullOrWhiteSpace(_settings.CaPath))
                            {
                                throw new InvalidOperationException(
                                    "mTLS requires a CA bundle (ASHLAR_MESH_SERVE_CA) to validate client certs against.");
                            }
                            var ca = MeshTls.LoadCaBundle(_settings.CaPath);
                            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            https.ClientCertificateValidation = (cert, _, _) => MeshTls.ChainsToCa(cert, ca);
                        }
                    });
                }
            });
        });

        var app = builder.Build();

        app.MapGet("/mesh/v1/hello", () =>
        {
            string fingerprint;
            try { fingerprint = OperatorKey.TryLoad()?.Fingerprint ?? "(unsigned)"; }
            catch { fingerprint = "(unsigned)"; }
            return Results.Json(new
            {
                version = MeshWire.Version,
                name = _settings.NodeName,
                fingerprint,
                packages = ListPackages().Count,
            });
        });

        app.MapGet("/mesh/v1/index", () => Results.Json(ListPackages()));

        app.MapGet("/mesh/v1/pkg/{file}", (string file) =>
        {
            if (!MeshWire.IsSafePackageName(file))
            {
                return Results.NotFound();
            }
            var full = Path.GetFullPath(Path.Combine(_settings.PublishedDir, file));
            // Containment on the RESOLVED path, independent of the name check.
            if (!string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(_settings.PublishedDir), StringComparison.Ordinal)
                || !File.Exists(full)
                || new FileInfo(full).Length > MeshWire.MaxPackageBytes)
            {
                return Results.NotFound();
            }
            return Results.File(full, "application/json");   // Kestrel streams it under its own limits
        });

        return app;
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
}
