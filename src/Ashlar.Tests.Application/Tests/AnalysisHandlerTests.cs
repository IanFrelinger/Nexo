using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Core.Application.Analysis.UseCases.AnalyzeCode;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Values;

namespace Ashlar.Tests.Application.Tests;

/// <summary>
/// Tests for AnalyzeCodeHandler.
/// </summary>
public class AnalysisHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var mockService = new Mock<IAnalysisService>();
            var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();

            var handler = new AnalyzeCodeHandler(
                mockService.Object,
                mockLogger.Object);

            var testPath = new DirectoryInfo(Path.GetTempPath());
            var expectedResult = new AnalysisResult
            {
                HasViolations = false,
                Violations = Array.Empty<Violation>(),
                TotalViolations = 0
            };

            mockService
                .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var command = new AnalyzeCodeCommand(testPath);
            var result = await handler.Handle(command, cancellationToken);

            AssertNotNull(result);
            AssertFalse(result.HasViolations);
            AssertEqual(0, result.TotalViolations);

            return new TestResult
            {
                Name = nameof(AnalysisHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "Analysis handler tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(AnalysisHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

