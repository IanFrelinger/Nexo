using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace Ashlar.Tests.Orchestration.Architecture;

/// <summary>Tests for orchestration layering.</summary>
public sealed class OrchestrationLayeringTests
{
    [Fact]
    public void AshlarOrchestration_MustNotReference_AshlarRuntime()
    {
        var orchestrationProjectPath = GetOrchestrationProjectPath();
        File.Exists(orchestrationProjectPath).Should().BeTrue();

        var document = XDocument.Load(orchestrationProjectPath);
        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();

        projectReferences.Should().NotContain(reference =>
            reference.Contains("Ashlar.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetOrchestrationProjectPath()
    {
        var srcRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        return Path.GetFullPath(Path.Combine(
            srcRoot,
            "Ashlar.Orchestration/Ashlar.Orchestration.csproj"));
    }
}
