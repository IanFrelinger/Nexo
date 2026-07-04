using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using FluentAssertions;
using Nexo.Infrastructure.Testing;
using Nexo.Infrastructure.Testing.Docker;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Testing;

/// <summary>Tests for tar archive helper gap coverage.</summary>
public sealed class TarArchiveHelperGapCoverageTests
{
    [Fact]
    public void CreateBuildContextTar_includes_dockerfile_and_context_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tar-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dockerfile = Path.Combine(root, "Dockerfile");
        File.WriteAllText(dockerfile, "FROM scratch");
        File.WriteAllText(Path.Combine(root, "global.json"), """{"sdk":{"version":"8.0.0"}}""");

        var srcDir = Path.Combine(root, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "Program.cs"), "Console.WriteLine();");

        try
        {
            using var tarGz = TarArchiveHelper.CreateBuildContextTar(root, dockerfile);
            using var gzip = new GZipStream(tarGz, CompressionMode.Decompress, leaveOpen: true);
            using var reader = new TarReader(gzip);

            var entries = new List<string>();
            while (reader.GetNextEntry() is { } entry)
            {
                if (!string.IsNullOrEmpty(entry.Name))
                    entries.Add(entry.Name.Replace('\\', '/'));
            }

            entries.Should().Contain("Dockerfile");
            entries.Should().Contain("global.json");
            entries.Should().Contain(entry => entry.Contains("src/Program.cs", StringComparison.Ordinal));
        }
        finally
        {
            /// <summary>Attempts to delete directory; returns false on failure.</summary>
            TryDeleteDirectory(root);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
