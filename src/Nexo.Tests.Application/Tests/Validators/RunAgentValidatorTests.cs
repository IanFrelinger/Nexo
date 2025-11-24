using FluentValidation;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Validators;

public class RunAgentValidatorTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var validator = new RunAgentValidator();

            // Test 1: Null agent name should fail
            var command1 = new RunAgentCommand(null!, null);
            var result1 = validator.Validate(command1);
            AssertFalse(result1.IsValid, "Null agent name should fail validation");
            AssertTrue(result1.Errors.Any(e => e.PropertyName == "AgentName" && e.ErrorMessage == "Agent name is required"),
                "Should have 'Agent name is required' error");

            // Test 2: Empty string agent name should fail
            var command2 = new RunAgentCommand(string.Empty, null);
            var result2 = validator.Validate(command2);
            AssertFalse(result2.IsValid, "Empty string agent name should fail validation");
            AssertTrue(result2.Errors.Any(e => e.PropertyName == "AgentName" && e.ErrorMessage == "Agent name is required"),
                "Should have 'Agent name is required' error");

            // Test 3: Whitespace-only agent name should fail
            var command3 = new RunAgentCommand("   ", null);
            var result3 = validator.Validate(command3);
            AssertFalse(result3.IsValid, "Whitespace-only agent name should fail validation");
            AssertTrue(result3.Errors.Any(e => e.PropertyName == "AgentName" && e.ErrorMessage == "Agent name is required"),
                "Should have 'Agent name is required' error");

            // Test 4: Valid agent name should pass
            var command4 = new RunAgentCommand("TestAgent", null);
            var result4 = validator.Validate(command4);
            AssertTrue(result4.IsValid, "Valid agent name should pass validation");
            AssertFalse(result4.Errors.Any(e => e.PropertyName == "AgentName"),
                "Should not have AgentName validation errors");

            // Test 5: Valid agent name with input file should pass
            var inputFile = new FileInfo(Path.GetTempFileName());
            var command5 = new RunAgentCommand("TestAgent", inputFile);
            var result5 = validator.Validate(command5);
            AssertTrue(result5.IsValid, "Valid agent name with input file should pass validation");
            AssertFalse(result5.Errors.Any(e => e.PropertyName == "AgentName"),
                "Should not have AgentName validation errors");

            // Test 6: Valid agent name with non-existent input file should still pass (file existence is not validated)
            var nonExistentFile = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            var command6 = new RunAgentCommand("TestAgent", nonExistentFile);
            var result6 = validator.Validate(command6);
            AssertTrue(result6.IsValid, "Valid agent name with non-existent input file should pass validation");
            AssertFalse(result6.Errors.Any(e => e.PropertyName == "AgentName"),
                "Should not have AgentName validation errors");

            return Task.FromResult(new TestResult
            {
                TestName = nameof(RunAgentValidatorTests),
                Category = "Application",
                Passed = true,
                Message = "All RunAgentValidator tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(RunAgentValidatorTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

