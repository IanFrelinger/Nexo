using System.Text.Json;
using FluentAssertions;
using Nexo.Commercial.GameDomain.Assets;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for network descriptor.</summary>
public class NetworkDescriptorTests
{
    [Fact]
    public void NetworkDescriptor_DefaultValues()
    {
        var net = new NetworkDescriptor();

        net.Id.Should().BeEmpty();
        net.Name.Should().BeEmpty();
        net.Category.Should().Be("player");
        net.IsPlayerObject.Should().BeFalse();
        net.DontDestroyWithOwner.Should().BeFalse();
        net.SyncedVariables.Should().BeEmpty();
        net.Rpcs.Should().BeEmpty();
        net.Spawning.Should().NotBeNull();
        net.Tags.Should().BeEmpty();
    }

    [Fact]
    public void NetworkVariable_WithPermissions()
    {
        var nv = new NetworkVariable
        {
            Name = "Health",
            Type = "float",
            Permission = "owner",
            DeliveryMode = "unreliable",
            SendRate = 10.0
        };

        nv.Name.Should().Be("Health");
        nv.Type.Should().Be("float");
        nv.Permission.Should().Be("owner");
        nv.DeliveryMode.Should().Be("unreliable");
        nv.SendRate.Should().Be(10.0);
    }

    [Fact]
    public void NetworkRpc_WithParameters()
    {
        var rpc = new NetworkRpc
        {
            Name = "ShootServerRpc",
            Direction = "server",
            DeliveryMode = "reliable",
            Parameters =
            [
                new RpcParameter { Name = "origin", Type = "Vector3" },
                new RpcParameter { Name = "direction", Type = "Vector3" },
                new RpcParameter { Name = "weaponId", Type = "int" }
            ]
        };

        rpc.Name.Should().Be("ShootServerRpc");
        rpc.Direction.Should().Be("server");
        rpc.Parameters.Should().HaveCount(3);
        rpc.Parameters[0].Name.Should().Be("origin");
        rpc.Parameters[2].Type.Should().Be("int");
    }

    [Fact]
    public void SpawnConfig_Defaults()
    {
        var spawn = new SpawnConfig();

        spawn.AutoSpawn.Should().BeTrue();
        spawn.SpawnAuthority.Should().Be("server");
        spawn.DespawnDelay.Should().Be(0.0);
        spawn.PoolId.Should().BeNull();
    }

    [Fact]
    public void FullPlayerNetworkDescriptor_RoundTrip()
    {
        var original = new NetworkDescriptor
        {
            Id = "net-player-01",
            Name = "FPS Player",
            Category = "player",
            IsPlayerObject = true,
            DontDestroyWithOwner = false,
            SyncedVariables =
            [
                new NetworkVariable { Name = "Health", Type = "float", Permission = "server", DeliveryMode = "reliable", SendRate = 0 },
                new NetworkVariable { Name = "Position", Type = "Vector3", Permission = "owner", DeliveryMode = "unreliable", SendRate = 20 }
            ],
            Rpcs =
            [
                new NetworkRpc
                {
                    Name = "ShootServerRpc",
                    Direction = "server",
                    Parameters = [new RpcParameter { Name = "dir", Type = "Vector3" }]
                },
                new NetworkRpc
                {
                    Name = "TakeDamageClientRpc",
                    Direction = "client",
                    Parameters = [new RpcParameter { Name = "amount", Type = "float" }]
                }
            ],
            Spawning = new SpawnConfig { AutoSpawn = true, SpawnAuthority = "server", DespawnDelay = 3.0, PoolId = "player_pool" },
            Tags = ["multiplayer", "fps"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<NetworkDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("net-player-01");
        restored.Name.Should().Be("FPS Player");
        restored.IsPlayerObject.Should().BeTrue();
        restored.SyncedVariables.Should().HaveCount(2);
        restored.SyncedVariables[0].Name.Should().Be("Health");
        restored.SyncedVariables[1].SendRate.Should().Be(20);
        restored.Rpcs.Should().HaveCount(2);
        restored.Rpcs[0].Parameters.Should().HaveCount(1);
        restored.Spawning.PoolId.Should().Be("player_pool");
        restored.Spawning.DespawnDelay.Should().Be(3.0);
        restored.Tags.Should().Contain("multiplayer");
    }
}
