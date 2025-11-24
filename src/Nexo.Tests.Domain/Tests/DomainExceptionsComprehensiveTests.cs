using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Comprehensive tests for all domain exceptions covering all constructors.
/// </summary>
public class DomainExceptionsComprehensiveTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test AnalysisException - all constructors
            TestAnalysisException();
            
            // Test ValidationException - all constructors
            TestValidationException();
            
            // Test AgentExecutionException - all constructors
            TestAgentExecutionException();
            
            // Test ConfigurationException - all constructors
            TestConfigurationException();

            return Task.FromResult(new TestResult
            {
                TestName = nameof(DomainExceptionsComprehensiveTests),
                Category = "Domain",
                Passed = true,
                Message = "All domain exception tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(DomainExceptionsComprehensiveTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestAnalysisException()
    {
        // Constructor 1: message only
        var ex1 = new AnalysisException("Message 1");
        AssertEqual("Message 1", ex1.Message);
        AssertNull(ex1.ErrorCode);
        AssertNull(ex1.Suggestion);
        AssertNull(ex1.InnerException);

        // Constructor 2: message + innerException
        var innerEx1 = new Exception("Inner exception");
        var ex2 = new AnalysisException("Message 2", innerEx1);
        AssertEqual("Message 2", ex2.Message);
        AssertNotNull(ex2.InnerException);
        AssertEqual("Inner exception", ex2.InnerException.Message);
        AssertNull(ex2.ErrorCode);
        AssertNull(ex2.Suggestion);

        // Constructor 3: message + errorCode
        var ex3 = new AnalysisException("Message 3", ErrorCodes.AnalysisPathNotFound);
        AssertEqual("Message 3", ex3.Message);
        AssertEqual(ErrorCodes.AnalysisPathNotFound, ex3.ErrorCode);
        AssertNull(ex3.Suggestion);
        AssertNull(ex3.InnerException);

        // Constructor 4: message + errorCode + suggestion
        var ex4 = new AnalysisException("Message 4", ErrorCodes.AnalysisUnauthorizedAccess, "Suggestion 4");
        AssertEqual("Message 4", ex4.Message);
        AssertEqual(ErrorCodes.AnalysisUnauthorizedAccess, ex4.ErrorCode);
        AssertEqual("Suggestion 4", ex4.Suggestion);
        AssertNull(ex4.InnerException);

        // Constructor 5: message + errorCode + innerException
        var innerEx2 = new Exception("Inner exception 2");
        var ex5 = new AnalysisException("Message 5", ErrorCodes.AnalysisInvalidAssembly, innerEx2);
        AssertEqual("Message 5", ex5.Message);
        AssertEqual(ErrorCodes.AnalysisInvalidAssembly, ex5.ErrorCode);
        AssertNotNull(ex5.InnerException);
        AssertNull(ex5.Suggestion);

        // Constructor 6: message + errorCode + innerException + suggestion
        var innerEx3 = new Exception("Inner exception 3");
        var ex6 = new AnalysisException("Message 6", ErrorCodes.AnalysisRuleFailure, innerEx3, "Suggestion 6");
        AssertEqual("Message 6", ex6.Message);
        AssertEqual(ErrorCodes.AnalysisRuleFailure, ex6.ErrorCode);
        AssertNotNull(ex6.InnerException);
        AssertEqual("Suggestion 6", ex6.Suggestion);
    }

    private void TestValidationException()
    {
        // Constructor 1: message only
        var ex1 = new ValidationException("Message 1");
        AssertEqual("Message 1", ex1.Message);
        AssertNull(ex1.ErrorCode);
        AssertNull(ex1.Suggestion);
        AssertNull(ex1.InnerException);

        // Constructor 2: message + innerException
        var innerEx1 = new Exception("Inner exception");
        var ex2 = new ValidationException("Message 2", innerEx1);
        AssertEqual("Message 2", ex2.Message);
        AssertNotNull(ex2.InnerException);
        AssertEqual("Inner exception", ex2.InnerException.Message);
        AssertNull(ex2.ErrorCode);
        AssertNull(ex2.Suggestion);

        // Constructor 3: message + errorCode
        var ex3 = new ValidationException("Message 3", ErrorCodes.ValidationNoTestProjects);
        AssertEqual("Message 3", ex3.Message);
        AssertEqual(ErrorCodes.ValidationNoTestProjects, ex3.ErrorCode);
        AssertNull(ex3.Suggestion);
        AssertNull(ex3.InnerException);

        // Constructor 4: message + errorCode + suggestion
        var ex4 = new ValidationException("Message 4", ErrorCodes.ValidationTestExecutionFailed, "Suggestion 4");
        AssertEqual("Message 4", ex4.Message);
        AssertEqual(ErrorCodes.ValidationTestExecutionFailed, ex4.ErrorCode);
        AssertEqual("Suggestion 4", ex4.Suggestion);
        AssertNull(ex4.InnerException);

        // Constructor 5: message + errorCode + innerException
        var innerEx2 = new Exception("Inner exception 2");
        var ex5 = new ValidationException("Message 5", ErrorCodes.ValidationTrxParseFailed, innerEx2);
        AssertEqual("Message 5", ex5.Message);
        AssertEqual(ErrorCodes.ValidationTrxParseFailed, ex5.ErrorCode);
        AssertNotNull(ex5.InnerException);
        AssertNull(ex5.Suggestion);

        // Constructor 6: message + errorCode + innerException + suggestion
        var innerEx3 = new Exception("Inner exception 3");
        var ex6 = new ValidationException("Message 6", ErrorCodes.ValidationTimeout, innerEx3, "Suggestion 6");
        AssertEqual("Message 6", ex6.Message);
        AssertEqual(ErrorCodes.ValidationTimeout, ex6.ErrorCode);
        AssertNotNull(ex6.InnerException);
        AssertEqual("Suggestion 6", ex6.Suggestion);
    }

    private void TestAgentExecutionException()
    {
        // Constructor 1: agentName + message
        var ex1 = new AgentExecutionException("test-agent", "Message 1");
        AssertEqual("test-agent", ex1.AgentName);
        AssertEqual("Message 1", ex1.Message);
        AssertNull(ex1.ErrorCode);
        AssertNull(ex1.Suggestion);
        AssertNull(ex1.InnerException);

        // Constructor 2: agentName + message + innerException
        var innerEx1 = new Exception("Inner exception");
        var ex2 = new AgentExecutionException("test-agent", "Message 2", innerEx1);
        AssertEqual("test-agent", ex2.AgentName);
        AssertEqual("Message 2", ex2.Message);
        AssertNotNull(ex2.InnerException);
        AssertEqual("Inner exception", ex2.InnerException.Message);
        AssertNull(ex2.ErrorCode);
        AssertNull(ex2.Suggestion);

        // Constructor 3: agentName + message + errorCode
        var ex3 = new AgentExecutionException("test-agent", "Message 3", ErrorCodes.AgentNotFound);
        AssertEqual("test-agent", ex3.AgentName);
        AssertEqual("Message 3", ex3.Message);
        AssertEqual(ErrorCodes.AgentNotFound, ex3.ErrorCode);
        AssertNull(ex3.Suggestion);
        AssertNull(ex3.InnerException);

        // Constructor 4: agentName + message + errorCode + suggestion
        var ex4 = new AgentExecutionException("test-agent", "Message 4", ErrorCodes.AgentExecutionFailed, "Suggestion 4");
        AssertEqual("test-agent", ex4.AgentName);
        AssertEqual("Message 4", ex4.Message);
        AssertEqual(ErrorCodes.AgentExecutionFailed, ex4.ErrorCode);
        AssertEqual("Suggestion 4", ex4.Suggestion);
        AssertNull(ex4.InnerException);

        // Constructor 5: agentName + message + errorCode + innerException
        var innerEx2 = new Exception("Inner exception 2");
        var ex5 = new AgentExecutionException("test-agent", "Message 5", ErrorCodes.AgentTimeout, innerEx2);
        AssertEqual("test-agent", ex5.AgentName);
        AssertEqual("Message 5", ex5.Message);
        AssertEqual(ErrorCodes.AgentTimeout, ex5.ErrorCode);
        AssertNotNull(ex5.InnerException);
        AssertNull(ex5.Suggestion);

        // Constructor 6: agentName + message + errorCode + innerException + suggestion
        var innerEx3 = new Exception("Inner exception 3");
        var ex6 = new AgentExecutionException("test-agent", "Message 6", ErrorCodes.AgentInvalidInput, innerEx3, "Suggestion 6");
        AssertEqual("test-agent", ex6.AgentName);
        AssertEqual("Message 6", ex6.Message);
        AssertEqual(ErrorCodes.AgentInvalidInput, ex6.ErrorCode);
        AssertNotNull(ex6.InnerException);
        AssertEqual("Suggestion 6", ex6.Suggestion);
    }

    private void TestConfigurationException()
    {
        // Constructor 1: message only
        var ex1 = new ConfigurationException("Message 1");
        AssertEqual("Message 1", ex1.Message);
        AssertNull(ex1.ErrorCode);
        AssertNull(ex1.Suggestion);
        AssertNull(ex1.InnerException);

        // Constructor 2: message + innerException
        var innerEx1 = new Exception("Inner exception");
        var ex2 = new ConfigurationException("Message 2", innerEx1);
        AssertEqual("Message 2", ex2.Message);
        AssertNotNull(ex2.InnerException);
        AssertEqual("Inner exception", ex2.InnerException.Message);
        AssertNull(ex2.ErrorCode);
        AssertNull(ex2.Suggestion);

        // Constructor 3: message + errorCode
        var ex3 = new ConfigurationException("Message 3", ErrorCodes.ConfigFileNotFound);
        AssertEqual("Message 3", ex3.Message);
        AssertEqual(ErrorCodes.ConfigFileNotFound, ex3.ErrorCode);
        AssertNull(ex3.Suggestion);
        AssertNull(ex3.InnerException);

        // Constructor 4: message + errorCode + suggestion
        var ex4 = new ConfigurationException("Message 4", ErrorCodes.ConfigInvalidFormat, "Suggestion 4");
        AssertEqual("Message 4", ex4.Message);
        AssertEqual(ErrorCodes.ConfigInvalidFormat, ex4.ErrorCode);
        AssertEqual("Suggestion 4", ex4.Suggestion);
        AssertNull(ex4.InnerException);

        // Constructor 5: message + errorCode + innerException
        var innerEx2 = new Exception("Inner exception 2");
        var ex5 = new ConfigurationException("Message 5", ErrorCodes.ConfigInvalidValue, innerEx2);
        AssertEqual("Message 5", ex5.Message);
        AssertEqual(ErrorCodes.ConfigInvalidValue, ex5.ErrorCode);
        AssertNotNull(ex5.InnerException);
        AssertNull(ex5.Suggestion);

        // Constructor 6: message + errorCode + innerException + suggestion
        var innerEx3 = new Exception("Inner exception 3");
        var ex6 = new ConfigurationException("Message 6", ErrorCodes.ConfigFileNotFound, innerEx3, "Suggestion 6");
        AssertEqual("Message 6", ex6.Message);
        AssertEqual(ErrorCodes.ConfigFileNotFound, ex6.ErrorCode);
        AssertNotNull(ex6.InnerException);
        AssertEqual("Suggestion 6", ex6.Suggestion);
    }
}

