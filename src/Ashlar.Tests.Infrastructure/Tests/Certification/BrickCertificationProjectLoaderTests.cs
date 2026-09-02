using FluentAssertions;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// What the loader is allowed to call "the brick".
///
/// <para>Everything downstream treats the loaded source text AS the brick: it is what the
/// analyzer fence judges, what the mutation leg mutates, and what the signed content hash
/// covers. The loader used to take <c>GetFiles("*.cs").FirstOrDefault(...)</c> — one file out of
/// however many, in filesystem order — so a brick spanning several files was certified against
/// an arbitrarily chosen one while the rest sat outside the signed hash entirely, and renaming a
/// helper could flip the verdict. These facts pin the refusal that replaced it.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class BrickCertificationProjectLoaderTests : IDisposable
{
    private readonly string _dir;

    public BrickCertificationProjectLoaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "Brick.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task A_multi_file_brick_is_refused_by_name_not_certified_from_one_file()
    {
        Write("BrickPart.cs", "public sealed class BrickPart { }");
        Write("Helper.cs", "public static class Helper { }");

        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        // Both files named, so the refusal is actionable without a directory listing, and it
        // must not be the build failure that a silently-chosen file would have produced.
        thrown.WithMessage("*Multi-file brick refused*")
            .And.Message.Should().Contain("BrickPart.cs").And.Contain("Helper.cs");
    }

    [Fact]
    public void The_source_set_is_every_authored_file_and_only_those()
    {
        Write("BrickPart.cs", "public sealed class BrickPart { }");
        Write("Nested/Helper.cs", "public static class Helper { }");
        // Build outputs are not authored brick text and must not turn a single-file brick into a
        // refused multi-file one.
        Write("obj/Debug/Brick.AssemblyInfo.cs", "// generated");
        Write("obj/Debug/Brick.GlobalUsings.g.cs", "// generated");
        Write("bin/Debug/Leftover.cs", "// stale output");

        var found = BrickCertificationProjectLoader.FindBrickSourceFiles(_dir)
            .Select(f => Path.GetFileName(f))
            .ToArray();

        found.Should().BeEquivalentTo(["BrickPart.cs", "Helper.cs"]);
    }

    [Fact]
    public void A_subdirectory_file_counts_as_part_of_the_brick()
    {
        // Non-recursive enumeration was the quieter half of the same defect: a helper under a
        // subdirectory was neither certified nor noticed. It has to make the set.
        Write("BrickPart.cs", "public sealed class BrickPart { }");
        Write("Helpers/Extra.cs", "public static class Extra { }");

        BrickCertificationProjectLoader.FindBrickSourceFiles(_dir).Should().HaveCount(2);
    }

    [Fact]
    public async Task A_project_with_no_source_is_refused_with_the_fix_named()
    {
        var act = async () => await BrickCertificationProjectLoader.LoadAsync(
            _dir, Path.Combine(_dir, "witness.json"));

        (await act.Should().ThrowAsync<FileNotFoundException>())
            .And.Message.Should().Contain("Fix:");
    }

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}
