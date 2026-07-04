using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Mesh;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Mesh;

/// <summary>Tests for file based instance discovery.</summary>
public sealed class FileBasedInstanceDiscoveryTests : IDisposable
{
    private readonly string _path;
    private readonly string? _prevMeshPolicy;
    private readonly string? _prevInstancesPath;

    public FileBasedInstanceDiscoveryTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"nexo-instances-{Guid.NewGuid():N}.json");
        _prevMeshPolicy = Environment.GetEnvironmentVariable("NEXO_MESH_TRUST_POLICY");
        _prevInstancesPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
    }

    public void Dispose()
    {
        /// <summary>Restore env.</summary>
        RestoreEnv("NEXO_MESH_TRUST_POLICY", _prevMeshPolicy);
        /// <summary>Restore env.</summary>
        RestoreEnv("NEXO_MESH_INSTANCES_PATH", _prevInstancesPath);
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
            // best effort
        }
    }

    private static void RestoreEnv(string key, string? value)
    {
        if (value is null)
            Environment.SetEnvironmentVariable(key, null);
        else
            Environment.SetEnvironmentVariable(key, value);
    }

    [Fact]
    public async Task Allowlist_policy_excludes_non_admitted_peers()
    {
        var json = """
                   [
                     { "peerId": "a", "endpoint": "http://a", "capabilities": [], "trustTier": "Trusted", "admitted": true },
                     { "peerId": "b", "endpoint": "http://b", "capabilities": [], "trustTier": "Trusted", "admitted": false },
                     { "peerId": "c", "endpoint": "http://c", "capabilities": [], "trustTier": "Trusted", "admitted": true, "drained": true }
                   ]
                   """;
        await File.WriteAllTextAsync(_path, json);
        Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", _path);
        Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", "allowlist");

        IInstanceDiscovery sut = new FileBasedInstanceDiscovery(_path, null, null, "any");
        var peers = await sut.DiscoverAsync();

        peers.Should().HaveCount(1);
        peers[0].PeerId.Should().Be("a");
        peers[0].Admitted.Should().BeTrue();
    }

    [Fact]
    public async Task Any_policy_includes_non_admitted_when_not_drained()
    {
        var json = """
                   [
                     { "peerId": "a", "endpoint": "http://a", "capabilities": [], "admitted": false },
                     { "peerId": "b", "endpoint": "http://b", "capabilities": [], "admitted": true }
                   ]
                   """;
        await File.WriteAllTextAsync(_path, json);
        Environment.SetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH", _path);
        Environment.SetEnvironmentVariable("NEXO_MESH_TRUST_POLICY", "any");

        IInstanceDiscovery sut = new FileBasedInstanceDiscovery(_path, null, null, "any");
        var peers = await sut.DiscoverAsync();

        peers.Should().HaveCount(2);
        peers.Should().Contain(p => p.PeerId == "a");
        peers.Should().Contain(p => p.PeerId == "b");
    }
}
