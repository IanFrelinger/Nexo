using FluentAssertions;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Guards;
using Nexo.Spike.S0.Models;
using Xunit;

namespace Nexo.Tests.Spike.S0;

public sealed class WorkspaceRedStageTests
{
    private static string Fixture(string relative) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", relative));

    [Fact]
    public async Task Honest_tests_fail_against_stub_with_assertion_reason()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-red-" + Guid.NewGuid().ToString("N"));
        SpikeWorkspaceScaffold.CreateFresh(workspace, overwrite: false);
        File.Copy(
            Fixture("red/honest-tests.cs"),
            Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerTests.cs"),
            overwrite: true);
        File.Delete(Path.Combine(workspace, "CsvColumnInferrer.Tests", "ColumnTypeInferrerPlaceholderTests.cs"));

        var (buildCode, _, _, buildTimedOut) = await SpikeWorkspaceScaffold.BuildAsync(workspace);
        buildCode.Should().Be(0);
        buildTimedOut.Should().BeFalse();

        var (testCode, testOut, testErr, testTimedOut) = await SpikeWorkspaceScaffold.TestAsync(workspace);
        testTimedOut.Should().BeFalse();
        testCode.Should().Be(1, because: testOut + testErr);

        var reason = RedReasonClassifier.Classify(testCode, testOut, testErr, testTimedOut);
        reason.Should().Be(RedFailureReason.AssertionFailure);
    }
}
