using System.Text.Json;
using Nexo.GameDomain.Session;

namespace GameDirector.Domain;

/// <summary>
/// Multi-asset Forge session wrapper: balance sheets, map configs, and generation history.
/// </summary>
public sealed class GameProjectContext
{
    public string SessionId { get; set; } = string.Empty;
    public SessionState ForgeSession { get; set; } = new();
    public List<BalanceSheetRef> BalanceSheets { get; set; } = [];
    public List<MapConfigRef> MapConfigs { get; set; } = [];
    public List<GenerationHistoryEntry> GenerationHistory { get; set; } = [];

    public void AttachToSessionMetadata()
    {
        ForgeSession.Metadata["gameDirector.balanceSheets"] = JsonSerializer.Serialize(BalanceSheets);
        ForgeSession.Metadata["gameDirector.mapConfigs"] = JsonSerializer.Serialize(MapConfigs);
        ForgeSession.Metadata["gameDirector.generationHistory"] = JsonSerializer.Serialize(GenerationHistory);
        ForgeSession.LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public static GameProjectContext FromSession(SessionState session)
    {
        var ctx = new GameProjectContext
        {
            SessionId = session.SessionId,
            ForgeSession = session
        };

        if (session.Metadata.TryGetValue("gameDirector.balanceSheets", out var balanceJson))
        {
            ctx.BalanceSheets = JsonSerializer.Deserialize<List<BalanceSheetRef>>(balanceJson) ?? [];
        }

        if (session.Metadata.TryGetValue("gameDirector.mapConfigs", out var mapJson))
        {
            ctx.MapConfigs = JsonSerializer.Deserialize<List<MapConfigRef>>(mapJson) ?? [];
        }

        if (session.Metadata.TryGetValue("gameDirector.generationHistory", out var genJson))
        {
            ctx.GenerationHistory = JsonSerializer.Deserialize<List<GenerationHistoryEntry>>(genJson) ?? [];
        }

        return ctx;
    }
}

public sealed record BalanceSheetRef(string SheetId, string Path, DateTimeOffset LastSeenUtc);
public sealed record MapConfigRef(string MapId, string Path, DateTimeOffset LastValidatedUtc);
public sealed record GenerationHistoryEntry(
    string AuditId,
    string TargetType,
    string Prompt,
    DateTimeOffset CreatedAtUtc);
