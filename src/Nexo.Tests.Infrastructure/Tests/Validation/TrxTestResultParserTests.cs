using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;
using Nexo.Core.Application.Validation.Models;
using Nexo.Infrastructure.Validation.Parsers;
using Nexo.Tests.Application.Helpers;
using System.Xml.Linq;

namespace Nexo.Tests.Infrastructure.Tests.Validation;

public class TrxTestResultParserTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
        return Task.CompletedTask;
    }

    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestSupportedExtensions();
            await TestNonExistentFile();
            await TestValidTrxFileWithPassedTests();
            await TestValidTrxFileWithFailedTests();
            await TestValidTrxFileWithMixedResults();
            await TestInvalidXmlFile();
            await TestEmptyTrxFile();
            await TestCancellation();

            return new TestingTestResult
            {
                TestName = nameof(TrxTestResultParserTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All TrxTestResultParser tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(TrxTestResultParserTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(TrxTestResultParserTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private void TestSupportedExtensions()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        AssertNotNull(parser.SupportedExtensions);
        AssertEqual(1, parser.SupportedExtensions.Count);
        AssertEqual(".trx", parser.SupportedExtensions[0]);
    }

    private async Task TestNonExistentFile()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var nonExistentFile = new FileInfo(Path.Combine(_tempDir!.FullName, "nonexistent.trx"));
        var results = await parser.ParseAsync(nonExistentFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(0, results.Count);
    }

    private async Task TestValidTrxFileWithPassedTests()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var trxContent = CreateTrxFileContent(new (string Name, string Outcome, string? ErrorMessage)[]
        {
            ("Test1", "Passed", null),
            ("Test2", "Passed", null)
        });

        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "test.trx"));
        File.WriteAllText(trxFile.FullName, trxContent);

        var results = await parser.ParseAsync(trxFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(2, results.Count);
        AssertTrue(results.All(r => r.Passed));
        AssertTrue(results.Any(r => r.Name == "Test1"));
        AssertTrue(results.Any(r => r.Name == "Test2"));
    }

    private async Task TestValidTrxFileWithFailedTests()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var trxContent = CreateTrxFileContent(new (string Name, string Outcome, string? ErrorMessage)[]
        {
            ("Test1", "Failed", "Assertion failed: expected true"),
            ("Test2", "Failed", "Null reference exception")
        });

        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "test.trx"));
        File.WriteAllText(trxFile.FullName, trxContent);

        var results = await parser.ParseAsync(trxFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(2, results.Count);
        AssertTrue(results.All(r => !r.Passed));
        AssertTrue(results.Any(r => r.Name == "Test1" && r.Message == "Assertion failed: expected true"));
        AssertTrue(results.Any(r => r.Name == "Test2" && r.Message == "Null reference exception"));
    }

    private async Task TestValidTrxFileWithMixedResults()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var trxContent = CreateTrxFileContent(new (string Name, string Outcome, string? ErrorMessage)[]
        {
            ("Test1", "Passed", null),
            ("Test2", "Failed", "Test failed"),
            ("Test3", "Passed", null)
        });

        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "test.trx"));
        File.WriteAllText(trxFile.FullName, trxContent);

        var results = await parser.ParseAsync(trxFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(3, results.Count);
        AssertEqual(2, results.Count(r => r.Passed));
        AssertEqual(1, results.Count(r => !r.Passed));
        AssertTrue(results.Any(r => r.Name == "Test1" && r.Passed));
        AssertTrue(results.Any(r => r.Name == "Test2" && !r.Passed));
        AssertTrue(results.Any(r => r.Name == "Test3" && r.Passed));
    }

    private async Task TestInvalidXmlFile()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var invalidXml = "<invalid><xml>";
        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "invalid.trx"));
        File.WriteAllText(trxFile.FullName, invalidXml);

        // Should handle gracefully and return empty results
        var results = await parser.ParseAsync(trxFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(0, results.Count);
    }

    private async Task TestEmptyTrxFile()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var trxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Results>
  </Results>
</TestRun>";

        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "empty.trx"));
        File.WriteAllText(trxFile.FullName, trxContent);

        var results = await parser.ParseAsync(trxFile, CancellationToken.None);

        AssertNotNull(results);
        AssertEqual(0, results.Count);
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<TrxTestResultParser>>();
        var parser = new TrxTestResultParser(mockLogger.Object);

        var trxContent = CreateTrxFileContent(new (string Name, string Outcome, string? ErrorMessage)[]
        {
            ("Test1", "Passed", null)
        });

        var trxFile = new FileInfo(Path.Combine(_tempDir!.FullName, "test.trx"));
        File.WriteAllText(trxFile.FullName, trxContent);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should handle cancellation gracefully
        var results = await parser.ParseAsync(trxFile, cts.Token);

        // May return empty results or partial results depending on when cancellation occurs
        AssertNotNull(results);
    }

    private string CreateTrxFileContent((string Name, string Outcome, string? ErrorMessage)[] tests)
    {
        var ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
        var root = new XElement(ns + "TestRun",
            new XElement(ns + "Results",
                tests.Select((test, index) =>
                {
                    var unitTestResult = new XElement(ns + "UnitTestResult",
                        new XAttribute("testName", test.Name),
                        new XAttribute("outcome", test.Outcome),
                        new XAttribute("duration", "00:00:01.000"),
                        new XAttribute("startTime", "2024-01-01T00:00:00.000Z"),
                        new XAttribute("endTime", "2024-01-01T00:00:01.000Z"),
                        new XAttribute("testId", $"test-id-{index}"));

                    if (test.Outcome == "Failed" && test.ErrorMessage != null)
                    {
                        unitTestResult.Add(new XElement(ns + "Output",
                            new XElement(ns + "ErrorInfo",
                                new XElement(ns + "Message", test.ErrorMessage))));
                    }

                    return unitTestResult;
                })
            ),
            new XElement(ns + "TestDefinitions",
                tests.Select((test, index) =>
                    new XElement(ns + "UnitTest",
                        new XAttribute("id", $"test-id-{index}"),
                        new XAttribute("name", test.Name),
                        new XElement(ns + "TestCategory",
                            new XElement(ns + "TestCategoryItem",
                                new XAttribute("TestCategory", "UnitTest"))))
                )
            )
        );

        return root.ToString();
    }
}

