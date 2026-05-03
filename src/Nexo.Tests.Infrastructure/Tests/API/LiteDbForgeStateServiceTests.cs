using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.API.Forge;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class LiteDbForgeStateServiceTests
{
    [Fact]
    public void RoundTrip_PersistsSessionAndMacros()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-forge-test-{Guid.NewGuid():N}.db");
        try
        {
            SessionState createdSession;
            using (var first = new LiteDbForgeStateService(path, NullLoggerFactory.Instance))
            {
                createdSession = new SessionState
                {
                    SessionId = "persist-1",
                    Name = "LiteDb Session",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    LastModifiedAtUtc = DateTimeOffset.UtcNow,
                    MaxPlayers = 12,
                };
                first.Session = createdSession;
                first.Registry.Register(new MacroDefinition { MacroId = "m1", DisplayName = "One" });
                first.Save();
            }

            using (var second = new LiteDbForgeStateService(path, NullLoggerFactory.Instance))
            {
                second.Session.SessionId.Should().Be("persist-1");
                second.Session.Name.Should().Be("LiteDb Session");
                second.Session.MaxPlayers.Should().Be(12);
                second.Registry.List().Should().ContainSingle(m => m.MacroId == "m1");
            }
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup in temp.
        }
    }
}
