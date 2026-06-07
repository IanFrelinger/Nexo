using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;

namespace Nexo.API.Forge;

/// <summary>
/// Isolates Forge session and macro state per tenant id (from <see cref="ForgeTenantMiddleware"/>),
/// using either in-memory slots or a separate LiteDB file per tenant under a common root directory.
/// </summary>
public sealed class TenantPartitionedForgeStateService : IForgeStateService, IDisposable
{
    private readonly IHttpContextAccessor _http;
    private readonly string? _resolvedLiteRoot;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, InMemoryForgeStateService> _memory = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LiteDbForgeStateService> _lite = new(StringComparer.Ordinal);

    public TenantPartitionedForgeStateService(
        IHttpContextAccessor http,
        IOptions<ForgeSessionOptions> options,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        _http = http;
        var opts = options.Value ?? new ForgeSessionOptions();
        _loggerFactory = loggerFactory;

        var path = opts.LiteDbPath?.Trim();
        if (!string.IsNullOrEmpty(path))
        {
            _resolvedLiteRoot = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, path));
        }
    }

    public SessionState Session
    {
        get => Resolve().Session;
        set => Resolve().Session = value;
    }

    public MacroRegistry Registry
    {
        get => Resolve().Registry;
        set => Resolve().Registry = value;
    }

    public void Save() => Resolve().Save();

    public void Dispose()
    {
        foreach (var db in _lite.Values)
            db.Dispose();
        _lite.Clear();
        _memory.Clear();
    }

    /// <summary>For tests: release all tenant LiteDB handles and clear in-memory tenants.</summary>
    internal void ResetAllTenantsForTests()
    {
        foreach (var db in _lite.Values)
            db.Dispose();
        _lite.Clear();
        _memory.Clear();
    }

    private IForgeStateService Resolve()
    {
        var tenantId = ForgeTenantResolver.CurrentTenantId(_http);
        if (string.IsNullOrEmpty(_resolvedLiteRoot))
            return _memory.GetOrAdd(tenantId, _ => new InMemoryForgeStateService());

        var tenantPath = ResolveTenantDatabasePath(tenantId);
        return _lite.GetOrAdd(tenantId, _ => new LiteDbForgeStateService(tenantPath, _loggerFactory));
    }

    private string ResolveTenantDatabasePath(string tenantId)
    {
        var root = _resolvedLiteRoot!;
        var directory = Path.GetDirectoryName(Path.GetFullPath(root));
        var fileName = Path.GetFileName(root);
        if (string.IsNullOrEmpty(directory))
            directory = Path.GetTempPath();

        var baseName = string.IsNullOrEmpty(fileName) ? "forge" : Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.HasExtension(root) ? Path.GetExtension(root) : ".db";
        var tenantDir = Path.Combine(directory, baseName + "-tenants", tenantId);
        return Path.Combine(tenantDir, "forge" + ext);
    }
}
