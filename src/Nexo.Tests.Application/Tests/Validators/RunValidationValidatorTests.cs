using FluentValidation;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Validation.UseCases.RunValidation;

namespace Nexo.Tests.Application.Tests.Validators;

/// <summary>Tests for run validation validator.</summary>
public class RunValidationValidatorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var validator = new RunValidationValidator();

            // Test 1: Null filter should pass (filter is optional)
            var command1 = new RunValidationCommand(null);
            var result1 = validator.Validate(command1);
            AssertTrue(result1.IsValid, "Null filter should pass validation (filter is optional)");
            AssertFalse(result1.Errors.Any(e => e.PropertyName == "Filter"),
                "Should not have Filter validation errors");

            // Test 2: Empty string filter should pass (filter is optional)
            var command2 = new RunValidationCommand(string.Empty);
            var result2 = validator.Validate(command2);
            AssertTrue(result2.IsValid, "Empty string filter should pass validation (filter is optional)");
            AssertFalse(result2.Errors.Any(e => e.PropertyName == "Filter"),
                "Should not have Filter validation errors");

            // Test 3: Whitespace filter should pass (filter is optional)
            var command3 = new RunValidationCommand("   ");
            var result3 = validator.Validate(command3);
            AssertTrue(result3.IsValid, "Whitespace filter should pass validation (filter is optional)");
            AssertFalse(result3.Errors.Any(e => e.PropertyName == "Filter"),
                "Should not have Filter validation errors");

            // Test 4: Valid filter string should pass
            var command4 = new RunValidationCommand("TestCategory");
            var result4 = validator.Validate(command4);
            /// <summary>Assert true.</summary>
            /// <param name="validation"">Validation".</param>
            AssertTrue(result4.IsValid, "Valid filter string should pass validation");
            AssertFalse(result4.Errors.Any(e => e.PropertyName == "Filter"),
                "Should not have Filter validation errors");

            // Test 5: Valid filter with progress should pass
            var progress = new Progress<Nexo.Core.Application.Common.Models.ProgressReport>();
            var command5 = new RunValidationCommand("TestCategory", progress);
            var result5 = validator.Validate(command5);
            /// <summary>Assert true.</summary>
            /// <param name="validation"">Validation".</param>
            AssertTrue(result5.IsValid, "Valid filter with progress should pass validation");
            AssertFalse(result5.Errors.Any(e => e.PropertyName == "Filter"),
                "Should not have Filter validation errors");

            return Task.FromResult(new TestResult
            {
                Name = nameof(RunValidationValidatorTests),
                Category = "Application",
                Passed = true,
                Message = "All RunValidationValidator tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(RunValidationValidatorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

