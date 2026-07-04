using FluentAssertions;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for trust policy pack registry gap coverage.</summary>
public sealed class TrustPolicyPackRegistryGapCoverageTests
{
    [Fact]
    public void GetById_returns_null_for_blank_id()
    {
        using var temp = CreatePackRoot();
        var registry = new TrustPolicyPackRegistry(temp.Path);

        registry.GetById("").Should().BeNull();
        registry.GetById("   ").Should().BeNull();
    }

    [Fact]
    public async Task ActivateAsync_throws_for_blank_or_unknown_id()
    {
        using var temp = CreatePackRoot();
        var registry = new TrustPolicyPackRegistry(temp.Path);

        var blank = () => registry.ActivateAsync("  ");
        await blank.Should().ThrowAsync<ArgumentException>().WithParameterName("id");

        var missing = () => registry.ActivateAsync("missing-pack");
        await missing.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing-pack*");
    }

    [Fact]
    public async Task ActivateAsync_defaults_blank_version_to_1_0_0()
    {
        using var temp = CreatePackRoot();
        await File.WriteAllTextAsync(
            Path.Combine(temp.Path, "no-version.json"),
            """
            {
              "id": "no-version",
              "version": "   ",
              "displayName": "No version",
              "description": "test",
              "pauseObservationByDefault": false,
              "categoryRules": {},
              "sourceRules": {},
              "projectRules": []
            }
            """);

        var registry = new TrustPolicyPackRegistry(temp.Path);
        var active = await registry.ActivateAsync("no-version");

        active.Version.Should().Be("1.0.0");
        registry.GetActivePack()!.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Reload_skips_empty_id_packs_and_active_pack_json()
    {
        using var temp = CreatePackRoot();
        File.WriteAllText(
            Path.Combine(temp.Path, "empty-id.json"),
            """
            {
              "id": "   ",
              "version": "1.0.0",
              "displayName": "Empty id",
              "description": "skip",
              "pauseObservationByDefault": false,
              "categoryRules": {},
              "sourceRules": {},
              "projectRules": []
            }
            """);
        File.WriteAllText(Path.Combine(temp.Path, "active-pack.json"), """{"id":"ignored"}""");
        File.WriteAllText(
            Path.Combine(temp.Path, "valid.json"),
            """
            {
              "id": "valid",
              "version": "1.0.0",
              "displayName": "Valid",
              "description": "ok",
              "pauseObservationByDefault": false,
              "categoryRules": {},
              "sourceRules": {},
              "projectRules": []
            }
            """);

        var registry = new TrustPolicyPackRegistry(temp.Path);

        registry.ListPacks().Should().ContainSingle(p => p.Id == "valid");
        registry.GetById("ignored").Should().BeNull();
    }

    [Fact]
    public void Reload_tolerates_corrupt_active_pack_state_file()
    {
        using var temp = CreatePackRoot();
        File.WriteAllText(Path.Combine(temp.Path, "active-pack.json"), "{bad");

        var registry = new TrustPolicyPackRegistry(temp.Path);

        registry.GetActivePack().Should().BeNull();
    }

    private static TempPackRoot CreatePackRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-trust-gap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        /// <summary>Temp pack root.</summary>
        return new TempPackRoot(path);
    }

    /// <summary>Tests for temp pack root.</summary>
    private sealed class TempPackRoot(string path) : IDisposable
    {
        /// <summary>Path.</summary>
        public string Path { get; } = path;

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
