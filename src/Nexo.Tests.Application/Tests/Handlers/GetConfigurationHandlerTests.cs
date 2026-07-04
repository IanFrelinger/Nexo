using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Configuration.Models;
using Nexo.Core.Application.Configuration.Ports;
using Nexo.Core.Application.Configuration.UseCases.GetConfiguration;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>Tests for get configuration handler.</summary>
public class GetConfigurationHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful configuration loading.</summary>
            await TestSuccessfulConfigurationLoading();
            /// <summary>Test cancellation.</summary>
            await TestCancellation();
            /// <summary>Test default configuration.</summary>
            await TestDefaultConfiguration();

            return new TestResult
            {
                Name = nameof(GetConfigurationHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All GetConfigurationHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(GetConfigurationHandlerTests),
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
                Name = nameof(GetConfigurationHandlerTests),
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Analysis);
        /// <summary>Assert equal.</summary>
        AssertEqual(25, result.Analysis.MaxComplexityThreshold);
        /// <summary>Assert true.</summary>
        AssertTrue(result.Analysis.EnableSecurityScan);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Validation);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Logging);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Analysis);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Validation);
        /// <summary>Assert not null.</summary>
        AssertNotNull(result.Logging);
    }
}

