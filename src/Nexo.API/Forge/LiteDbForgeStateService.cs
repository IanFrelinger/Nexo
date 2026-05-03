using LiteDB;
using Microsoft.Extensions.Logging;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;

namespace Nexo.API.Forge;

/// <summary>
/// LiteDB-backed Forge session and macro registry. Thread-safe for typical API concurrency.
/// </summary>
public sealed class LiteDbForgeStateService : IForgeStateService, IDisposable
{
    private const string CollectionName = "forge_state";
    private const string DocId = "singleton";

    private readonly ILogger _log;
    private readonly string _resolvedPath;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<ForgeStateDocument> _col;
    private readonly object _gate = new();

    private SessionState _session;
    private MacroRegistry _registry;

    public LiteDbForgeStateService(string liteDbPath, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _log = loggerFactory.CreateLogger<LiteDbForgeStateService>();
        if (string.IsNullOrWhiteSpace(liteDbPath))
            throw new ArgumentException("LiteDB path is required.", nameof(liteDbPath));

        _resolvedPath = liteDbPath.Trim();
        var dir = Path.GetDirectoryName(Path.GetFullPath(_resolvedPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _db = new LiteDatabase(_resolvedPath);
        _col = _db.GetCollection<ForgeStateDocument>(CollectionName);
        _col.EnsureIndex(x => x.Id, unique: true);

        lock (_gate)
        {
            var doc = _col.FindById(DocId);
            if (doc is null)
            {
                _session = InMemoryForgeStateService.CreateDefaultSession();
                _registry = new MacroRegistry();
                PersistUnlocked();
                _log.LogInformation("Initialized new Forge session store at {Path}.", _resolvedPath);
            }
            else
            {
                try
                {
                    _session = SessionExporter.ImportFromJson(doc.SessionJson);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to deserialize Forge session; using default session.");
                    _session = InMemoryForgeStateService.CreateDefaultSession();
                }

                _registry = new MacroRegistry();
                try
                {
                    foreach (var macro in MacroExporter.ImportMany(doc.MacrosJson))
                        _registry.Register(macro);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to deserialize Forge macros; starting with an empty registry.");
                }

                AlignSessionMacrosUnlocked();
            }
        }
    }

    public SessionState Session
    {
        get
        {
            lock (_gate)
                return _session;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_gate)
            {
                _session = value;
                _registry = new MacroRegistry();
                foreach (var m in _session.Macros)
                    _registry.Register(m);
                PersistUnlocked();
            }
        }
    }

    public MacroRegistry Registry
    {
        get
        {
            lock (_gate)
                return _registry;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_gate)
            {
                _registry = value;
                AlignSessionMacrosUnlocked();
                PersistUnlocked();
            }
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            AlignSessionMacrosUnlocked();
            PersistUnlocked();
        }
    }

    private void AlignSessionMacrosUnlocked()
    {
        _session.Macros.Clear();
        _session.Macros.AddRange(_registry.List());
    }

    private void PersistUnlocked()
    {
        var doc = new ForgeStateDocument
        {
            Id = DocId,
            SessionJson = SessionExporter.ExportToJson(_session),
            MacrosJson = MacroExporter.ExportMany(_registry.List())
        };
        _col.Upsert(doc);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private sealed class ForgeStateDocument
    {
        public string Id { get; set; } = string.Empty;
        public string SessionJson { get; set; } = "{}";
        public string MacrosJson { get; set; } = "[]";
    }
}
