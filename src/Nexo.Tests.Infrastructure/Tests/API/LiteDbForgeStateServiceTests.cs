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
        var path = Path.Combine(Path.GetTempPath(), $"nexo-forge-{Guid.NewGuid():N}.db");
        try
        {
            using (var first = new LiteDbForgeStateService(path, NullLoggerFactory.Instance))
            {
                first.Session = new SessionState
                {
                    SessionId = "s1",
                    Name = "Persisted",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    LastModifiedAtUtc = DateTimeOffset.UtcNow,
                    MaxPlayers = 11,
                };
                first.Registry.Register(new MacroDefinition { MacroId = "m1", DisplayName = "One" });
                first.Save();
            }

            using (var second = new LiteDbForgeStateService(path, NullLoggerFactory.Instance))
            {
                second.Session.SessionId.Should().Be("s1");
                second.Session.Name.Should().Be("Persisted");
                second.Session.MaxPlayers.Should().Be(11);
                second.Registry.List().Should().ContainSingle(m => m.MacroId == "m1");
            }
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
