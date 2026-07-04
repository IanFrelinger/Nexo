using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Domain;

/// <summary>
/// Guards the core Clean Architecture dependency direction:
/// Domain &lt;- Application &lt;- (Infrastructure / CLI / Runtime / Transport / API).
/// Inner layers must never take a project reference on an outer layer. These are
/// tripwires against accidental drift; loosening them requires an explicit rationale.
/// </summary>
public sealed class CoreLayeringArchitectureTests
{
    // Outer layers that Core.Domain (the innermost layer) must never reference.
    private static readonly string[] LayersForbiddenFromDomain =
    [
        "Nexo.Core.Application",
        "Nexo.Infrastructure",
        "Nexo.Orchestration",
        "Nexo.Runtime",
        "Nexo.CLI",
        "Nexo.API",
        "Nexo.Transport",
    ];

    // Outer layers that Core.Application must never reference (it may depend on Domain).
    private static readonly string[] LayersForbiddenFromApplication =
    [
        "Nexo.Infrastructure",
        "Nexo.Orchestration",
        "Nexo.Runtime",
        "Nexo.CLI",
        "Nexo.API",
        "Nexo.Transport",
    ];

    [Fact]
    public void CoreDomain_MustNotReference_OuterLayers()
    {
        var offenders = ForbiddenReferences("Nexo.Core.Domain", LayersForbiddenFromDomain);

        offenders.Should().BeEmpty(
            "Core.Domain is the innermost layer and must not depend on application, infrastructure, " +
            "orchestration, runtime, transport, CLI, or API projects.");
    }

    [Fact]
    public void CoreApplication_MustNotReference_OuterLayers()
    {
        var offenders = ForbiddenReferences("Nexo.Core.Application", LayersForbiddenFromApplication);

        offenders.Should().BeEmpty(
            "Core.Application may depend on Core.Domain but must not depend on infrastructure, " +
            "orchestration, runtime, transport, CLI, or API projects.");
    }

    private static IReadOnlyList<string> ForbiddenReferences(string projectName, string[] forbiddenLayers)
    {
        var projectPath = GetProjectPath(projectName);
        File.Exists(projectPath).Should().BeTrue($"expected to locate {projectName}.csproj at {projectPath}");

        var references = XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();

        return references
            .Where(reference => forbiddenLayers.Any(layer =>
                reference.Contains($"{layer}.csproj", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains($"{layer}\\", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains($"{layer}/", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetProjectPath(string projectName)
    {
        var srcRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        return Path.GetFullPath(Path.Combine(srcRoot, projectName, $"{projectName}.csproj"));
    }
}
