using FluentValidation;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Validators;

public class AnalyzeCodeValidatorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var validator = new AnalyzeCodeValidator();

            // Test 1: Null path should fail
            var command1 = new AnalyzeCodeCommand(null!);
            var result1 = validator.Validate(command1);
            AssertFalse(result1.IsValid, "Null path should fail validation");
            AssertNotNull(result1.Errors, "Validation errors should not be null");
            AssertTrue(result1.Errors.Count > 0, "Should have validation errors");
            AssertTrue(result1.Errors.Any(e => e.PropertyName == "Path" && e.ErrorMessage == "Path is required"),
                "Should have 'Path is required' error");

            // Test 2: Non-existent path should fail
            var nonExistentPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var command2 = new AnalyzeCodeCommand(nonExistentPath);
            var result2 = validator.Validate(command2);
            AssertFalse(result2.IsValid, "Non-existent path should fail validation");
            AssertTrue(result2.Errors.Any(e => e.PropertyName == "Path" && e.ErrorMessage == "Path must exist"),
                "Should have 'Path must exist' error");

            // Test 3: Valid existing path should pass
            var validPath = new DirectoryInfo(Path.GetTempPath());
            var command3 = new AnalyzeCodeCommand(validPath);
            var result3 = validator.Validate(command3);
            AssertTrue(result3.IsValid, "Valid path should pass validation");
            AssertFalse(result3.Errors.Any(e => e.PropertyName == "Path"),
                "Should not have Path validation errors");

            // Test 4: Valid path with progress should pass
            var progress = new Progress<Nexo.Core.Application.Common.Models.ProgressReport>();
            var command4 = new AnalyzeCodeCommand(validPath, progress);
            var result4 = validator.Validate(command4);
            AssertTrue(result4.IsValid, "Valid path with progress should pass validation");
            AssertFalse(result4.Errors.Any(e => e.PropertyName == "Path"),
                "Should not have Path validation errors");

            return Task.FromResult(new TestResult
            {
                Name = nameof(AnalyzeCodeValidatorTests),
                Category = "Application",
                Passed = true,
                Message = "All AnalyzeCodeValidator tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AnalyzeCodeValidatorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

