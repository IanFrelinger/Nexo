using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Validation.Models;
using Ashlar.Core.Application.Validation.Ports;
using Ashlar.Core.Application.Validation.UseCases.RunValidation;
using Ashlar.Core.Application.Common.Models;

namespace Ashlar.Tests.Application.Tests;

/// <summary>
/// Tests for RunValidationHandler.
/// </summary>
public class ValidationHandlerTests : UnitTestBase
{
    public override async Task<Ashlar.Core.Application.Common.Models.TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var mockService = new Mock<IValidationService>();
            var mockLogger = new Mock<ILogger<RunValidationHandler>>();

            var handler = new RunValidationHandler(
                mockService.Object,
                mockLogger.Object);

            var expectedResult = new ValidationResult
            {
                Passed = true,
                Message = "All tests passed",
                TestsRun = 10,
                TestsPassed = 10,
                TestsFailed = 0
            };

            mockService
                .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var command = new RunValidationCommand(null);
            var result = await handler.Handle(command, cancellationToken);

            AssertNotNull(result);
            AssertTrue(result.Passed);
            AssertEqual(10, result.TestsRun);
            AssertEqual(10, result.TestsPassed);

            return new Ashlar.Core.Application.Common.Models.TestResult
            {
                Name = nameof(ValidationHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "Validation handler tests passed"
            };
        }
        catch (Exception ex)
        {
            return new Ashlar.Core.Application.Common.Models.TestResult
            {
                Name = nameof(ValidationHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

