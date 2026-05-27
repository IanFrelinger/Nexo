using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Infrastructure.Validation.Parsers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Validation;

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
