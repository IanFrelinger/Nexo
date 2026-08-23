using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Infrastructure.Validation.Parsers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Validation;

/// <summary>Tests for trx test result parser gap coverage.</summary>
public class TrxTestResultParserGapCoverageTests
{
    [Fact]
    public async Task ParseAsync_returns_empty_when_file_missing()
    {
        var parser = new TrxTestResultParser(NullLogger<TrxTestResultParser>.Instance);
        var missing = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".trx"));

        var results = await parser.ParseAsync(missing, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_parses_passed_and_failed_results_with_category()
    {
        var path = Path.Combine(Path.GetTempPath(), "sample-" + Guid.NewGuid() + ".trx");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testName="Tests.Pass" testId="t1" outcome="Passed" duration="00:00:00.0100000" />
                <UnitTestResult testName="Tests.Fail" testId="t2" outcome="Failed" duration="00:00:00.0200000">
                  <Output>
                    <ErrorInfo>
                      <Message>boom</Message>
                    </ErrorInfo>
                  </Output>
                </UnitTestResult>
              </Results>
              <TestDefinitions>
                <UnitTest id="t1" name="Tests.Pass">
                  <TestCategory>
                    <TestCategoryItem TestCategory="Unit" />
                  </TestCategory>
                </UnitTest>
              </TestDefinitions>
            </TestRun>
            """);

        try
        {
            var parser = new TrxTestResultParser(NullLogger<TrxTestResultParser>.Instance);
            var results = await parser.ParseAsync(new FileInfo(path), CancellationToken.None);

            results.Should().HaveCount(2);
            results.Should().Contain(r => r.Name == "Tests.Pass" && r.Passed && r.Category == "Unit");
            results.Should().Contain(r => r.Name == "Tests.Fail" && !r.Passed && r.Message == "boom");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsync_reports_NotExecuted_as_skipped_not_failed_with_the_reason()
    {
        // A `[Fact(Skip = "...")]` gap marker lands in TRX as outcome="NotExecuted". It is
        // neither a pass nor a failure; reporting it as a failure is what kept a readiness
        // gate red on every commit for a documented, deliberate skip.
        var path = Path.Combine(Path.GetTempPath(), "skip-" + Guid.NewGuid() + ".trx");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testName="Tests.Gap" testId="t1" outcome="NotExecuted" duration="00:00:00.0010000">
                  <Output>
                    <ErrorInfo>
                      <Message>GAP: ceiling not implemented — see docs/AUDIT.md</Message>
                    </ErrorInfo>
                  </Output>
                </UnitTestResult>
                <UnitTestResult testName="Tests.Ok" testId="t2" outcome="Passed" duration="00:00:00.0010000" />
              </Results>
            </TestRun>
            """);

        try
        {
            var parser = new TrxTestResultParser(NullLogger<TrxTestResultParser>.Instance);
            var results = await parser.ParseAsync(new FileInfo(path), CancellationToken.None);

            results.Should().HaveCount(2, "skipped tests are reported, not dropped — the aggregator decides");
            var gap = results.Single(r => r.Name == "Tests.Gap");
            gap.Skipped.Should().BeTrue();
            gap.Passed.Should().BeFalse("a skip is not a pass either");
            gap.Message.Should().Contain("GAP: ceiling not implemented");
            results.Single(r => r.Name == "Tests.Ok").Skipped.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsync_returns_empty_on_invalid_xml()
    {
        var path = Path.Combine(Path.GetTempPath(), "bad-" + Guid.NewGuid() + ".trx");
        await File.WriteAllTextAsync(path, "not-xml");

        try
        {
            var parser = new TrxTestResultParser(NullLogger<TrxTestResultParser>.Instance);
            var results = await parser.ParseAsync(new FileInfo(path), CancellationToken.None);
            results.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
