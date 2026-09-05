using FluentAssertions;
using Ashlar.CLI.Commands;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Fail-closed coverage for multi-env empty <c>dotnet test</c> runs.</summary>
[Trait("Category", "CLI")]
public sealed class TestMultiEnvFailClosedTests
{
    [Theory]
    [InlineData("No test is available in Foo.dll.", false)]
    [InlineData("No test matches the given testcase filter", false)]
    [InlineData("Passed: 0, Failed: 0, Skipped: 0, Total: 0", false)]
    [InlineData("Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5", true)]
    [InlineData("the word passed appears but no counts", false)]
    public void EnvRunPassed_fails_closed_on_empty_runs(string output, bool expected)
    {
        TestMultiEnvCommand.EnvRunPassed(0, output).Should().Be(expected);
        TestMultiEnvCommand.EnvRunPassed(1, output).Should().BeFalse();
    }

    [Fact]
    public void ParseTestOutput_reads_vstest_summary()
    {
        var (passed, failed, total) = TestMultiEnvCommand.ParseTestOutput(
            "Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5");
        passed.Should().Be(5);
        failed.Should().Be(0);
        total.Should().Be(5);
    }

    [Fact]
    public void ParseTestOutput_does_not_fabricate_a_pass_from_the_word_passed()
    {
        var (passed, failed, total) = TestMultiEnvCommand.ParseTestOutput("the word passed appears");
        passed.Should().Be(0);
        failed.Should().Be(0);
        total.Should().Be(0);
    }
}
