using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Tests for domain exceptions.
/// </summary>
public class DomainExceptionsTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test AnalysisException
            var analysisEx = new AnalysisException(
                "Test message",
                ErrorCodes.AnalysisUnauthorizedAccess);
            AssertEqual("Test message", analysisEx.Message);
            AssertEqual(ErrorCodes.AnalysisUnauthorizedAccess, analysisEx.ErrorCode);

            // Test ValidationException
            var validationEx = new ValidationException(
                "Test message",
                ErrorCodes.ValidationNoTestProjects);
            AssertEqual("Test message", validationEx.Message);
            AssertEqual(ErrorCodes.ValidationNoTestProjects, validationEx.ErrorCode);

            // Test AgentExecutionException
            var agentEx = new AgentExecutionException(
                "test-agent",
                "Test message",
                ErrorCodes.AgentNotFound);
            AssertEqual("test-agent", agentEx.AgentName);
            AssertEqual("Test message", agentEx.Message);
            AssertEqual(ErrorCodes.AgentNotFound, agentEx.ErrorCode);

            // Test ConfigurationException
            var configEx = new ConfigurationException(
                "Test message",
                ErrorCodes.ConfigFileNotFound);
            AssertEqual("Test message", configEx.Message);
            AssertEqual(ErrorCodes.ConfigFileNotFound, configEx.ErrorCode);

            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainExceptionsTests),
                Category = "Domain",
                Passed = true,
                Message = "All domain exception tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainExceptionsTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

