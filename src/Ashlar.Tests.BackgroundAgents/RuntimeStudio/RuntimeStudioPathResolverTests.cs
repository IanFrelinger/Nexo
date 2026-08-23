using FluentAssertions;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.BackgroundAgents.RuntimeStudio;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.RuntimeStudio;

/// <summary>Tests for runtime studio path resolver.</summary>
public sealed class RuntimeStudioPathResolverTests : IDisposable
{
    private readonly string _base;

    public RuntimeStudioPathResolverTests()
    {
        _base = Path.Combine(Path.GetTempPath(), "ashlar-path-res-" + Guid.NewGuid().ToString("N"));
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
        p.ForgeRoot.Should().Be(Path.GetFullPath(Path.Combine(_base, ".ashlar", "runtime-studio", "forge")));
        p.ObservationsPath.Should().Be(Path.GetFullPath(Path.Combine(_base, JsonlObservationStore.DefaultRelativePath)));
    }
}
