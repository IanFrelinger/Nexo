using LiteDB;
using Microsoft.Extensions.Logging;
using Ashlar.Commercial.GameDomain.Descriptors;
using Ashlar.Commercial.GameDomain.Macros;
using Ashlar.Commercial.GameDomain.Session;

namespace GameDirector.Mcp.Forge;
/// <summary>Service for lite db forge state operations.</summary>
public sealed class LiteDbForgeStateService : IForgeStateService, IDisposable
{
    private const string Collection = "forge_state";
    private const string DocId = "singleton";

    private readonly ILogger _log;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<ForgeStateDoc> _col;
    private readonly object _gate = new();

    private SessionState _session;
    private MacroRegistry _registry;

    public LiteDbForgeStateService(string liteDbPath, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _log = loggerFactory.CreateLogger<LiteDbForgeStateService>();
        if (string.IsNullOrWhiteSpace(liteDbPath))
            throw new ArgumentException("LiteDB path is required.", nameof(liteDbPath));

        var path = liteDbPath.Trim();
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _db = new LiteDatabase(path);
        _col = _db.GetCollection<ForgeStateDoc>(Collection);
        _col.EnsureIndex(x => x.Id, unique: true);

        lock (_gate)
        {
            var doc = _col.FindById(DocId);
            if (doc is null)
            {
                _session = InMemoryForgeStateService.CreateDefaultSession();
                _registry = new MacroRegistry();
                /// <summary>Persist unlocked.</summary>
                PersistUnlocked();
                _log.LogInformation("Forge LiteDB store initialized at {Path}.", path);
            }
            else
            {
                try
                {
                    _session = SessionExporter.ImportFromJson(doc.SessionJson);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to deserialize Forge session; using default.");
                    _session = InMemoryForgeStateService.CreateDefaultSession();
                }

                _registry = new MacroRegistry();
                try
                {
                    foreach (var m in MacroExporter.ImportMany(doc.MacrosJson))
                        _registry.Register(m);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to deserialize macros; empty registry.");
                }

                /// <summary>Align session macros unlocked.</summary>
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
                /// <summary>Persist unlocked.</summary>
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
                /// <summary>Align session macros unlocked.</summary>
                AlignSessionMacrosUnlocked();
                /// <summary>Persist unlocked.</summary>
                PersistUnlocked();
            }
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            /// <summary>Align session macros unlocked.</summary>
            AlignSessionMacrosUnlocked();
            /// <summary>Persist unlocked.</summary>
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
        _col.Upsert(new ForgeStateDoc
        {
            Id = DocId,
            SessionJson = SessionExporter.ExportToJson(_session),
            MacrosJson = MacroExporter.ExportMany(_registry.List())
        });
    }

    /// <summary>Dispose.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>Forge state doc.</summary>
    private sealed class ForgeStateDoc
    {
        /// <summary>Id.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Session json.</summary>
        public string SessionJson { get; set; } = "{}";
        /// <summary>Macros json.</summary>
        public string MacrosJson { get; set; } = "[]";
    }
}
