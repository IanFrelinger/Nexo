using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Configuration.Models;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Application.Configuration.UseCases.GetConfiguration;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Handlers;

public class GetConfigurationHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulConfigurationLoading();
            await TestCancellation();
            await TestDefaultConfiguration();

            return new TestResult
            {
                TestName = nameof(GetConfigurationHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All GetConfigurationHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(GetConfigurationHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GetConfigurationHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulConfigurationLoading()
    {
        var mockConfigurationService = new Mock<IConfigurationService>();
        var mockLogger = new Mock<ILogger<GetConfigurationHandler>>();
        var handler = new GetConfigurationHandler(mockConfigurationService.Object, mockLogger.Object);

        var expectedConfiguration = new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration
            {
                MaxComplexityThreshold = 25,
                EnableSecurityScan = true,
                EnableCodeQuality = true
            },
            Validation = new ValidationConfiguration(),
            Logging = new LoggingConfiguration
            {
                Level = "Debug",
                EnableStructuredLogging = true
            }
        };

        mockConfigurationService
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfiguration);

        var query = new GetConfigurationQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        AssertNotNull(result);
        AssertNotNull(result.Analysis);
        AssertEqual(25, result.Analysis.MaxComplexityThreshold);
        AssertTrue(result.Analysis.EnableSecurityScan);
        AssertNotNull(result.Validation);
        AssertNotNull(result.Logging);
        AssertEqual("Debug", result.Logging.Level);

        mockConfigurationService.Verify(s => s.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestCancellation()
    {
        var mockConfigurationService = new Mock<IConfigurationService>();
        var mockLogger = new Mock<ILogger<GetConfigurationHandler>>();
        var handler = new GetConfigurationHandler(mockConfigurationService.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockConfigurationService
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetConfigurationQuery();

        await AssertThrowsAsync<OperationCanceledException>(() => handler.Handle(query, cts.Token));
    }

    private async Task TestDefaultConfiguration()
    {
        var mockConfigurationService = new Mock<IConfigurationService>();
        var mockLogger = new Mock<ILogger<GetConfigurationHandler>>();
        var handler = new GetConfigurationHandler(mockConfigurationService.Object, mockLogger.Object);

        var defaultConfiguration = new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration(),
            Validation = new ValidationConfiguration(),
            Logging = new LoggingConfiguration()
        };

        mockConfigurationService
            .Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultConfiguration);

        var query = new GetConfigurationQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        AssertNotNull(result);
        AssertNotNull(result.Analysis);
        AssertNotNull(result.Validation);
        AssertNotNull(result.Logging);
    }
}

