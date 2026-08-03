using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Application.Ide;

/// <summary>Guards that IdeEndpoints keeps the extension contract surface.</summary>
public sealed class IdeEndpointSurfaceConventionTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln"))
                || File.Exists(Path.Combine(dir.FullName, "application", "src", "Nexo.API", "Endpoints", "IdeEndpoints.cs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repo root from " + Directory.GetCurrentDirectory());
    }

    [Fact]
    public void IdeEndpoints_source_declares_required_routes()
    {
        var path = Path.Combine(FindRepoRoot(), "application", "src", "Nexo.API", "Endpoints", "IdeEndpoints.cs");
        File.Exists(path).Should().BeTrue(because: path);
        var src = File.ReadAllText(path);

        var required = new[]
        {
            "\"/health\"",
            "\"/models\"",
            "\"/agents\"",
            "\"/session\"",
            "\"/chat\"",
            "\"/chat/stream\"",
            "\"/edit\"",
            "\"/plan\"",
            "\"/director\"",
            "\"/runs\"",
            "\"/runs/{runId}\"",
            "\"/runs/{runId}/cancel\"",
        };

        foreach (var route in required)
            src.Should().Contain(route, because: $"IDE route {route} must stay mapped");

        src.Should().Contain("IdeFeatures", because: "health should advertise extension features");
        src.Should().Contain("text/event-stream", because: "chat stream must be SSE");
    }
}
