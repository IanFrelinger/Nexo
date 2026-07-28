using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class QtHeaderShimsTests
{
    [Fact]
    public void EnsureForSource_emits_QThread_shim()
    {
        var dir = Path.Combine(Path.GetTempPath(), "qt-shim-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var src = "#include <QThread>\n#include <QMessageBox>\n";
            var shims = QtHeaderShims.EnsureForSource(src, dir);
            shims.Keys.Should().Contain(new[] { "QThread", "QMessageBox", "QString" });
            File.Exists(Path.Combine(dir, "QThread")).Should().BeTrue();
            var shim = File.ReadAllText(Path.Combine(dir, "QThread"));
            shim.Should().Contain("class QThread");

            // The shim must not DECLARE Q_OBJECT (moc remains a hard stop), but its
            // own comment says so in prose — assert against the code, not the prose.
            CodeOf(shim).Should().NotContain("Q_OBJECT");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    /// <summary>Strips line comments so assertions target emitted code, not commentary.</summary>
    private static string CodeOf(string source) =>
        string.Join(
            "\n",
            source.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
}
