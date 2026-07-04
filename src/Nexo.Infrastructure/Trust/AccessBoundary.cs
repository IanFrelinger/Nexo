using System.Collections.Concurrent;
using System.Text.Json;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Trust;

/// <summary>
/// In-memory access boundary with optional JSON file persistence.
/// </summary>
public sealed class AccessBoundary : IAccessBoundary
{
    private readonly string? _configPath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private ActiveTrustPolicyPack? _activePolicyPack;

    private volatile bool _isPaused;
    private readonly ConcurrentDictionary<string, bool> _categoryAllowed = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _sourceAllowed = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, bool>> _projectOverrides = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Creates an access boundary. When configPath is set, loads/saves state to JSON.
    /// </summary>
    /// <param name="configPath">Optional path to JSON config file. If null, uses in-memory only.</param>
    public AccessBoundary(string? configPath = null)
    {
        _configPath = configPath;
        if (!string.IsNullOrWhiteSpace(_configPath) && File.Exists(_configPath))
            LoadFromFile();
    }

    /// <inheritdoc />
    public bool IsObservationPaused => _isPaused;

    /// <inheritdoc />
    public bool IsCategoryAllowed(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;
        return _categoryAllowed.TryGetValue(category.Trim(), out var allowed) ? allowed : true;
    }

    /// <inheritdoc />
    public bool IsSourceAllowed(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;
        return _sourceAllowed.TryGetValue(sourceId.Trim(), out var allowed) ? allowed : true;
    }

    /// <inheritdoc />
    public bool IsSourceAllowedForProject(string sourceId, string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        if (!string.IsNullOrWhiteSpace(projectPath) && _projectOverrides.TryGetValue(projectPath.Trim(), out var overrides) && overrides != null)
        {
            if (overrides.TryGetValue(sourceId.Trim(), out var projectAllowed))
                return projectAllowed;
        }

        return IsSourceAllowed(sourceId);
    }

    /// <inheritdoc />
    public void SetPause(bool paused)
    {
        if (_isPaused == paused)
            return;
        var prev = _isPaused;
        _isPaused = paused;
        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "pause",
            PreviousState = prev ? "paused" : "resumed",
            NewState = paused ? "paused" : "resumed",
        });
        _ = SaveToFileAsync();
    }

    /// <inheritdoc />
    public void SetCategoryAllowed(string category, bool allowed)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;
        var key = category.Trim();
        var hadValue = _categoryAllowed.TryGetValue(key, out var prev);
        if (hadValue && prev == allowed)
            return;
        _categoryAllowed[key] = allowed;
        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "category",
            Category = key,
            PreviousState = hadValue ? (prev ? "allowed" : "denied") : null,
            NewState = allowed ? "allowed" : "denied",
        });
        _ = SaveToFileAsync();
    }

    /// <inheritdoc />
    public void SetSourceAllowed(string sourceId, bool allowed)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;
        var key = sourceId.Trim();
        var hadValue = _sourceAllowed.TryGetValue(key, out var prev);
        if (hadValue && prev == allowed)
            return;
        _sourceAllowed[key] = allowed;
        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "source",
            SourceId = key,
            PreviousState = hadValue ? (prev ? "allowed" : "denied") : null,
            NewState = allowed ? "allowed" : "denied",
        });
        _ = SaveToFileAsync();
    }

    /// <inheritdoc />
    public void SetProjectOverride(string projectPath, IReadOnlyDictionary<string, bool>? overrides)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return;
        var key = projectPath.Trim();
        if (overrides == null)
            _projectOverrides.TryRemove(key, out _);
        else
            _projectOverrides[key] = overrides;
        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "project",
            ProjectPath = key,
            NewState = overrides != null ? "overrides set" : "overrides cleared",
        });
        _ = SaveToFileAsync();
    }

    /// <inheritdoc />
    public event Action<BoundaryChangeEvent>? BoundaryChanged;

    /// <inheritdoc />
    public void Reset()
    {
        _isPaused = false;
        _categoryAllowed.Clear();
        _sourceAllowed.Clear();
        _projectOverrides.Clear();
        _activePolicyPack = null;
        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "reset",
            NewState = "default"
        });
        _ = SaveToFileAsync();
    }

    /// <inheritdoc />
    public ActiveTrustPolicyPack? GetActivePolicyPack()
    {
        return _activePolicyPack;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, bool> GetCategoryAllowlistSnapshot()
    {
        return new Dictionary<string, bool>(_categoryAllowed, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, bool> GetSourceAllowlistSnapshot()
    {
        return new Dictionary<string, bool>(_sourceAllowed, StringComparer.OrdinalIgnoreCase);
    }

    private void RaiseBoundaryChanged(BoundaryChangeEvent evt)
    {
        BoundaryChanged?.Invoke(evt);
    }

    /// <inheritdoc />
    public void ApplyPolicyPack(TrustPolicyPack pack)
    {
        if (pack == null)
            throw new ArgumentNullException(nameof(pack));

        var normalizedId = pack.Id.Trim();
        if (string.IsNullOrWhiteSpace(normalizedId))
            throw new ArgumentException("Trust policy pack id is required.", nameof(pack));

        var previousPack = _activePolicyPack;

        _isPaused = pack.PauseObservationByDefault;
        _categoryAllowed.Clear();
        _sourceAllowed.Clear();
        _projectOverrides.Clear();

        foreach (var kv in pack.CategoryRules)
            _categoryAllowed[kv.Key] = kv.Value;

        foreach (var kv in pack.SourceRules)
            _sourceAllowed[kv.Key] = kv.Value;

        foreach (var projectRule in pack.ProjectRules)
        {
            _projectOverrides[projectRule.ProjectPath] = new Dictionary<string, bool>(
                projectRule.SourceRules,
                StringComparer.OrdinalIgnoreCase);
        }

        _activePolicyPack = new ActiveTrustPolicyPack(
            normalizedId,
            string.IsNullOrWhiteSpace(pack.Version) ? "1.0.0" : pack.Version.Trim(),
            DateTimeOffset.UtcNow);

        RaiseBoundaryChanged(new BoundaryChangeEvent
        {
            ChangeType = "policy-pack",
            PreviousState = previousPack is null
                ? null
                : $"{previousPack.Id}@{previousPack.Version}",
            NewState = $"{_activePolicyPack.Id}@{_activePolicyPack.Version}",
        });

        _ = SaveToFileAsync();
    }

    private void LoadFromFile()
    {
        try
        {
            var json = File.ReadAllText(_configPath!);
            var model = JsonSerializer.Deserialize<AccessBoundaryConfig>(json);
            if (model == null)
                return;
            _isPaused = model.IsPaused;
            foreach (var kv in model.CategoryAllowed ?? new Dictionary<string, bool>())
                _categoryAllowed[kv.Key] = kv.Value;
            foreach (var kv in model.SourceAllowed ?? new Dictionary<string, bool>())
                _sourceAllowed[kv.Key] = kv.Value;
            foreach (var kv in model.ProjectOverrides ?? new Dictionary<string, Dictionary<string, bool>>())
                _projectOverrides[kv.Key] = kv.Value;
            if (!string.IsNullOrWhiteSpace(model.ActivePolicyPackId))
            {
                _activePolicyPack = new ActiveTrustPolicyPack(
                    model.ActivePolicyPackId.Trim(),
                    string.IsNullOrWhiteSpace(model.ActivePolicyPackVersion) ? "1.0.0" : model.ActivePolicyPackVersion.Trim(),
                    DateTimeOffset.UtcNow);
            }
        }
        catch
        {
            // Ignore load errors; start fresh
        }
    }

    private async Task SaveToFileAsync()
    {
        if (string.IsNullOrWhiteSpace(_configPath))
            return;
        await _saveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var model = new AccessBoundaryConfig
            {
                IsPaused = _isPaused,
                CategoryAllowed = new Dictionary<string, bool>(_categoryAllowed),
                SourceAllowed = new Dictionary<string, bool>(_sourceAllowed),
                ProjectOverrides = _projectOverrides.ToDictionary(kv => kv.Key, kv => new Dictionary<string, bool>(kv.Value ?? new Dictionary<string, bool>()), StringComparer.OrdinalIgnoreCase),
                ActivePolicyPackId = _activePolicyPack?.Id,
                ActivePolicyPackVersion = _activePolicyPack?.Version
            };
            var json = JsonSerializer.Serialize(model, JsonOptions);
            await File.WriteAllTextAsync(_configPath!, json).ConfigureAwait(false);
        }
        catch
        {
            // Ignore save errors
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private sealed class AccessBoundaryConfig
    {
        /// <summary>Is paused.</summary>
        public bool IsPaused { get; set; }
        /// <summary>Category allowed.</summary>
        public Dictionary<string, bool>? CategoryAllowed { get; set; }
        /// <summary>Source allowed.</summary>
        public Dictionary<string, bool>? SourceAllowed { get; set; }
        /// <summary>Project overrides.</summary>
        public Dictionary<string, Dictionary<string, bool>>? ProjectOverrides { get; set; }
        /// <summary>Active policy pack id.</summary>
        public string? ActivePolicyPackId { get; set; }
        /// <summary>Active policy pack version.</summary>
        public string? ActivePolicyPackVersion { get; set; }
    }
}
