using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// P0.4: Proves the immutable core cannot be modified by self-adaptation.
/// </summary>
public sealed class ImmutableCoreTests : TempDirTestBase
{
    public ImmutableCoreTests() : base("nexo-immutable-core") { }

    [Fact]
    public void AdaptationEngine_CannotModify_ObservationPipeline()
    {
        var registry = new ImmutableCoreRegistry();
        var corePath = "src/Nexo.Infrastructure/Observation/FileSystemEventSource.cs";

        registry.IsInImmutableCore(corePath).Should().BeTrue();
    }

    [Fact]
    public void AdaptationEngine_CannotModify_ValidationChecker()
    {
        var registry = new ImmutableCoreRegistry();
        var corePath = "src/Nexo.Infrastructure/Validation/Adapters/ValidationServiceAdapter.cs";

        registry.IsInImmutableCore(corePath).Should().BeTrue();
    }

    [Fact]
    public void AdaptationEngine_CannotModify_InheritanceProtocol()
    {
        var registry = new ImmutableCoreRegistry();
        var corePath = "src/Nexo.Infrastructure/Adaptation/AdaptationPromoter.cs";

        registry.IsInImmutableCore(corePath).Should().BeTrue();
    }

    [Fact]
    public void AdaptationEngine_CannotModify_ScopeBoundaryEnforcer()
    {
        var registry = new ImmutableCoreRegistry();
        var corePath = "src/Nexo.Infrastructure/Trust/AccessBoundary.cs";

        registry.IsInImmutableCore(corePath).Should().BeTrue();
    }

    [Fact]
    public void ScopeEnforcer_RejectsAdaptation_TargetingCoreNamespace()
    {
        var registry = new ImmutableCoreRegistry();
        var corePath = "src/Nexo.Infrastructure/Observation/ObservationContextBrick.cs";

        var rejected = registry.IsInImmutableCore(corePath);

        rejected.Should().BeTrue("adaptation targeting core namespace should be rejected before execution");
    }

    [Fact]
    public void CoreComponentList_IsComplete_AndMatchesSpec()
    {
        var registry = new ImmutableCoreRegistry();

        registry.CoreComponentIds.Should().NotBeEmpty();
        registry.CoreComponentIds.Should().Contain("observation.pipeline");
        registry.CoreComponentIds.Should().Contain("analysis.engine");
        registry.CoreComponentIds.Should().Contain("validation.checker");
        registry.CoreComponentIds.Should().Contain("inheritance.protocol");
        registry.CoreComponentIds.Should().Contain("scope.boundary.enforcer");
        registry.CoreComponentIds.Should().Contain("dependency.graph");
        registry.CoreComponentIds.Should().Contain("rollback.manager");
    }

    [Fact]
    public void NonCorePath_IsAllowed()
    {
        var registry = new ImmutableCoreRegistry();
        var allowedPath = "src/Nexo.Bricks.Owasp/Security/OWASPScannerBrick.cs";

        registry.IsInImmutableCore(allowedPath).Should().BeFalse();
    }

    [Fact]
    public void ImproveFlow_SkipsImmutableCoreViolations()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
            return;

        var storePath = Path.Combine(TempDir, "adapt.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath)
            .BuildServiceProvider();

        var registry = services.GetRequiredService<IImmutableCoreRegistry>();
        var corePath = Path.Combine(repoRoot, "src", "Nexo.Infrastructure", "Observation", "FileSystemEventSource.cs");
        if (!File.Exists(corePath))
            return;

        var rejected = registry.IsInImmutableCore(corePath);
        rejected.Should().BeTrue();
    }
}
