using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Tests for domain exceptions.
/// </summary>
public class DomainExceptionsTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test AnalysisException
            var analysisEx = new AnalysisException(
                "Test message",
                ErrorCodes.AnalysisUnexpectedError);
            AssertEqual("Test message", analysisEx.Message);
            AssertNotNull(analysisEx.ErrorCode);
            AssertEqual(ErrorCodes.AnalysisUnexpectedError.Code, analysisEx.ErrorCode.Code);

            // Test ValidationException
            var validationEx = new ValidationException(
                "Test message",
                ErrorCodes.ValidationUnexpectedError);
            AssertEqual("Test message", validationEx.Message);
            AssertNotNull(validationEx.ErrorCode);

            // Test AgentExecutionException
            var agentEx = new AgentExecutionException(
                "test-agent",
                "Test message",
                ErrorCodes.AgentUnexpectedError);
            AssertEqual("test-agent", agentEx.AgentName);
            AssertEqual("Test message", agentEx.Message);
            AssertNotNull(agentEx.ErrorCode);

            // Test ConfigurationException
            var configEx = new ConfigurationException(
                "Test message",
                ErrorCodes.ConfigurationUnexpectedError);
            AssertEqual("Test message", configEx.Message);
            AssertNotNull(configEx.ErrorCode);

            return new TestResult
            {
                TestName = nameof(DomainExceptionsTests),
                Category = "Domain",
                Passed = true,
                Message = "All domain exception tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(DomainExceptionsTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

