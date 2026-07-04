using FluentAssertions;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Observations;
using Nexo.BackgroundAgents.RuntimeStudio;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RuntimeStudio;

/// <summary>Tests for runtime studio path resolver.</summary>
public sealed class RuntimeStudioPathResolverTests : IDisposable
{
    private readonly string _base;

    public RuntimeStudioPathResolverTests()
    {
        _base = Path.Combine(Path.GetTempPath(), "nexo-path-res-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_base);
    }

    public void Dispose()
    {
        try { Directory.Delete(_base, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Resolve_without_env_uses_base_directory()
    {
        var p = RuntimeStudioPathResolver.Resolve(_base);

        p.ObjectivesRoot.Should().Be(Path.GetFullPath(Path.Combine(_base, ObjectiveStore.DefaultRelativePath)));
        p.ForgeRoot.Should().Be(Path.GetFullPath(Path.Combine(_base, ".nexo", "runtime-studio", "forge")));
        p.ObservationsPath.Should().Be(Path.GetFullPath(Path.Combine(_base, JsonlObservationStore.DefaultRelativePath)));
    }
}
