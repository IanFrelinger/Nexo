using FluentAssertions;
using Nexo.Infrastructure.Skills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillContentHashCacheControllerTests
{
    [Fact]
    public void InvalidateIfContentChanged_bumps_generation_when_files_change()
    {
        using var temp = new TempDir();
        var controller = new SkillContentHashCacheController();
        File.WriteAllText(Path.Combine(temp.Path, "SKILL.md"), "v1");

        var first = controller.ComputeContentHash(temp.Path);
        controller.InvalidateIfContentChanged(temp.Path);
        File.WriteAllText(Path.Combine(temp.Path, "SKILL.md"), "v2-changed");
        controller.InvalidateIfContentChanged(temp.Path);
        var second = controller.ComputeContentHash(temp.Path);

        second.Should().NotBe(first);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexo-skill-cache-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
