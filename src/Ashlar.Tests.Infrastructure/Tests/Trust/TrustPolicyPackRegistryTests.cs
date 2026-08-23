using System;
using System.IO;
using FluentAssertions;
using Ashlar.Infrastructure.Trust;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for trust policy pack registry.</summary>
public sealed class TrustPolicyPackRegistryTests
{
    [Fact]
    public async Task ActivateAsync_PersistsActivePack_AndAccessBoundaryAppliesRules()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ashlar-trust-packs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var pack = """
{
  "id": "strict-enterprise",
  "version": "1.0.0",
  "displayName": "Strict enterprise",
  "description": "Strict profile",
  "pauseObservationByDefault": false,
  "categoryRules": { "terminal-output": false },
  "sourceRules": { "shell": false },
  "projectRules": []
}
""";
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "strict-enterprise.json"), pack);

            var boundary = new AccessBoundary();
            var registry = new TrustPolicyPackRegistry(tempRoot);
            var available = registry.ListPacks();
            available.Should().ContainSingle(x => x.Id == "strict-enterprise");

            var activated = await registry.ActivateAsync("strict-enterprise");
            activated.Id.Should().Be("strict-enterprise");

            var activeDefinition = registry.GetById("strict-enterprise");
            activeDefinition.Should().NotBeNull();
            boundary.ApplyPolicyPack(activeDefinition!);

            boundary.IsCategoryAllowed("terminal-output").Should().BeFalse();
            boundary.IsSourceAllowed("shell").Should().BeFalse();
            boundary.GetActivePolicyPack().Should().NotBeNull();
            boundary.GetActivePolicyPack()!.Id.Should().Be("strict-enterprise");
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // ignore cleanup failures in tests
            }
        }
    }
}
