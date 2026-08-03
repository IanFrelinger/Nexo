using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Application.Ide;

public sealed class IdePathSafetyConventionTests
{
    [Theory]
    [InlineData("src/Foo.cs", true)]
    [InlineData("a/b/c.ts", true)]
    [InlineData("../secrets", false)]
    [InlineData("a/../../etc/passwd", false)]
    [InlineData("/abs/path", false)]
    [InlineData("C:/Windows", false)]
    [InlineData("", false)]
    public void RelativePathRules(string path, bool expectedSafe)
    {
        IsSafeRelativePath(path).Should().Be(expectedSafe);
    }

    private static bool IsSafeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var p = path.Replace('\\', '/').Trim();
        if (p.StartsWith('/') || p.Contains(':', StringComparison.Ordinal))
            return false;
        return !p.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(s => s == "..");
    }
}
