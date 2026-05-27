using FluentAssertions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Infrastructure.IO;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests;

public class InfrastructureIoGapCoverageTests
{
    [Fact]
    public async Task LocalTextFileSystem_reads_and_writes_text_and_bytes()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-io-" + Guid.NewGuid() + ".txt");
        var fs = new LocalTextFileSystem();

        await fs.WriteAllTextAsync(path, "hello");
        (await fs.ReadAllTextAsync(path)).Should().Be("hello");

        await fs.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 });
        (await File.ReadAllBytesAsync(path)).Should().Equal(new byte[] { 1, 2, 3 });

        if (File.Exists(path)) File.Delete(path);
    }

    [Fact]
    public async Task TextFile_static_helpers_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-text-" + Guid.NewGuid() + ".txt");
        try
        {
            await TextFile.WriteAllTextAsync(path, "line1\nline2");
            TextFile.ReadAllText(path).Should().Contain("line1");
            (await TextFile.ReadAllLinesAsync(path)).Should().HaveCount(2);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task BinaryFile_static_helpers_write_bytes()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-bin-" + Guid.NewGuid() + ".bin");
        try
        {
            BinaryFile.Exists(path).Should().BeFalse();
            await BinaryFile.WriteAllBytesAsync(path, new byte[] { 9, 8 });
            await BinaryFile.WriteAllBytesAsync(path, new ReadOnlyMemory<byte>(new byte[] { 7 }));
            BinaryFile.Exists(path).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DirectoryOps_creates_parent_directories()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-dir-" + Guid.NewGuid());
        var nested = Path.Combine(root, "a", "b", "file.txt");
        try
        {
            DirectoryOps.EnsureParentDirectoryExists(nested);
            Directory.Exists(Path.GetDirectoryName(nested)!).Should().BeTrue();

            DirectoryOps.CreateDirectory(root);
            Directory.Exists(root).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LocalTextFileSystem_implements_text_file_system_port()
    {
        typeof(LocalTextFileSystem).Should().Implement<ITextFileSystem>();
    }
}
