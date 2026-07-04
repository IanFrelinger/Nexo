using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for lite db mesh director persistence.</summary>
[Collection(nameof(LiteDbFleetCollection))]
public sealed class LiteDbMeshDirectorPersistenceTests
{
    [Fact]
    public async Task Fleet_and_tasks_survive_new_registry_instance_same_database()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-mesh-{Guid.NewGuid():N}.db");
        try
        {
            MeshTaskState created;
            {
                var nodes1 = new LiteDbFleetNodeRegistry(path);
                var tasks1 = new LiteDbMeshTaskRegistry(path);

                await nodes1.RegisterOrUpdateAsync(new MeshFleetNodeState(
                    "persist-peer",
                    "https://worker.example/",
                    new Dictionary<string, string>(),
                    Array.Empty<string>(),
                    false,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    0,
                    MeshFleetTrustTier.Trusted,
                    Admitted: true,
                    RegistrationKeyFingerprint: "ABC123"));

                created = await tasks1.CreateAsync(
                    new MeshTaskCreateSpec(
                        "persist-task",
                        1,
                        Array.Empty<string>(),
                        null,
                        0,
                        null,
                        IdempotencyKey: "idem-persist-1"));
            }

            var nodes2 = new LiteDbFleetNodeRegistry(path);
            var tasks2 = new LiteDbMeshTaskRegistry(path);

            var node = await nodes2.GetAsync("persist-peer");
            node.Should().NotBeNull();
            node!.RegistrationKeyFingerprint.Should().Be("ABC123");

            var task = await tasks2.GetAsync(created.TaskId);
            task.Should().NotBeNull();
            task!.Name.Should().Be("persist-task");
            task.IdempotencyKey.Should().Be("idem-persist-1");

            var sameIdem = await tasks2.TryGetByIdempotencyKeyAsync("idem-persist-1");
            sameIdem!.TaskId.Should().Be(created.TaskId);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
