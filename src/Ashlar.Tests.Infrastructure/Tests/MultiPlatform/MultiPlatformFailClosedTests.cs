using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.MultiPlatform;

/// <summary>Fail-closed coverage for multi-platform empty <c>dotnet test</c> runs.</summary>
public sealed class MultiPlatformFailClosedTests
{
    [Theory]
    [InlineData("No test is available in Foo.dll.", false)]
    [InlineData("No test matches the given testcase filter", false)]
    [InlineData("Passed: 0, Failed: 0, Skipped: 0, Total: 0", false)]
    [InlineData("Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5", true)]
    [InlineData("the word passed appears but no counts", false)]
    public void RunPassed_fails_closed_on_empty_runs(string output, bool expected)
    {
        MultiPlatformTestBase.RunPassed(processSuccess: true, output).Should().Be(expected);
        MultiPlatformTestBase.RunPassed(processSuccess: false, output).Should().BeFalse();
    }

    [Fact]
    public void ParseDotNetCounts_reads_vstest_summary()
    {
        var output = "Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5";
        var (passed, failed, total) = MultiPlatformTestBase.ParseDotNetCounts(output);
        passed.Should().Be(5);
        failed.Should().Be(0);
        total.Should().Be(5);
    }

    [Fact]
    public void ParseDotNetCounts_does_not_fabricate_a_pass_from_the_word_passed()
    {
        var (passed, failed, total) = MultiPlatformTestBase.ParseDotNetCounts("the word passed appears");
        passed.Should().Be(0);
        failed.Should().Be(0);
        total.Should().Be(0);
    }
}
