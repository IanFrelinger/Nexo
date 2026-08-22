using FluentAssertions;
using GameDirector.Domain;
using Ashlar.Commercial.GameDomain.Session;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for game project context.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class GameProjectContextTests
{
    [Fact]
    public void AttachToSessionMetadata_round_trips_balance_and_map_refs()
    {
        var session = new SessionState
        {
            SessionId = "proj-1",
            Name = "Smoke Project",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var ctx = new GameProjectContext
        {
            SessionId = "proj-1",
            ForgeSession = session,
            BalanceSheets = [new BalanceSheetRef("sheet_a", "data/balance/a.json", DateTimeOffset.UtcNow)],
            MapConfigs = [new MapConfigRef("map_b", "data/maps/b.mapconfig", DateTimeOffset.UtcNow)],
            GenerationHistory =
            [
                new GenerationHistoryEntry("audit-1", "flavor", "prompt", DateTimeOffset.UtcNow)
            ]
        };

        ctx.AttachToSessionMetadata();
        var restored = GameProjectContext.FromSession(session);

        restored.BalanceSheets.Should().ContainSingle(b => b.SheetId == "sheet_a");
        restored.MapConfigs.Should().ContainSingle(m => m.MapId == "map_b");
        restored.GenerationHistory.Should().ContainSingle(g => g.AuditId == "audit-1");
        restored.ForgeSession.LastModifiedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void FromSession_empty_metadata_yields_empty_collections()
    {
        var session = new SessionState { SessionId = "empty" };
        var ctx = GameProjectContext.FromSession(session);

        ctx.BalanceSheets.Should().BeEmpty();
        ctx.MapConfigs.Should().BeEmpty();
        ctx.GenerationHistory.Should().BeEmpty();
    }
}
