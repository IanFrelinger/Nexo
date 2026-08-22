using FluentAssertions;
using Ashlar.Infrastructure.Execution.Scratch;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Execution;

public sealed class ManagedFileSetTests
{
    [Fact]
    public async Task ApplyAsync_WritesViaTempSwap_AndRollbackRestoresPrior()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "mfs-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            var existing = Path.Combine(root, "adapted_reader.hpp");
            await File.WriteAllTextAsync(existing, "OLD");

            var set = new SnapshotManagedFileSet();
            var apply = await set.ApplyAsync(
                root,
                new Dictionary<string, string>
                {
                    ["adapted_reader.hpp"] = "NEW",
                    ["Extra.hpp"] = "companion"
                });

            apply.Succeeded.Should().BeTrue();
            (await File.ReadAllTextAsync(existing)).Should().Be("NEW");
            (await File.ReadAllTextAsync(Path.Combine(root, "Extra.hpp"))).Should().Be("companion");

            await apply.Rollback(CancellationToken.None);

            (await File.ReadAllTextAsync(existing)).Should().Be("OLD");
            File.Exists(Path.Combine(root, "Extra.hpp")).Should().BeFalse();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ApplyAsync_RejectsPathEscape()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "mfs-" + Guid.NewGuid().ToString("n"))).FullName;
        try
        {
            var set = new SnapshotManagedFileSet();
            var act = async () => await set.ApplyAsync(
                root,
                new Dictionary<string, string> { ["../escape.txt"] = "x" });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
