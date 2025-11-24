using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;
using System.Reflection;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Tests for ErrorCodes class to verify all constants are defined, format consistency, and uniqueness.
/// </summary>
public class ErrorCodesTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test all error codes are defined
            TestAllErrorCodesDefined();
            
            // Test format consistency
            TestFormatConsistency();
            
            // Test uniqueness
            TestUniqueness();

            return Task.FromResult(new TestResult
            {
                TestName = nameof(ErrorCodesTests),
                Category = "Domain",
                Passed = true,
                Message = "All error code tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(ErrorCodesTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestAllErrorCodesDefined()
    {
        // Analysis errors
        AssertNotNull(ErrorCodes.AnalysisPathNotFound);
        AssertNotNull(ErrorCodes.AnalysisUnauthorizedAccess);
        AssertNotNull(ErrorCodes.AnalysisInvalidAssembly);
        AssertNotNull(ErrorCodes.AnalysisRuleFailure);

        // Validation errors
        AssertNotNull(ErrorCodes.ValidationNoTestProjects);
        AssertNotNull(ErrorCodes.ValidationTestExecutionFailed);
        AssertNotNull(ErrorCodes.ValidationTrxParseFailed);
        AssertNotNull(ErrorCodes.ValidationTimeout);

        // Agent errors
        AssertNotNull(ErrorCodes.AgentNotFound);
        AssertNotNull(ErrorCodes.AgentExecutionFailed);
        AssertNotNull(ErrorCodes.AgentTimeout);
        AssertNotNull(ErrorCodes.AgentInvalidInput);

        // Configuration errors
        AssertNotNull(ErrorCodes.ConfigFileNotFound);
        AssertNotNull(ErrorCodes.ConfigInvalidFormat);
        AssertNotNull(ErrorCodes.ConfigInvalidValue);

        // General errors
        AssertNotNull(ErrorCodes.GeneralUnexpectedError);
        AssertNotNull(ErrorCodes.GeneralInvalidArgument);

        // Verify they are not empty
        AssertTrue(ErrorCodes.AnalysisPathNotFound.Length > 0);
        AssertTrue(ErrorCodes.ValidationNoTestProjects.Length > 0);
        AssertTrue(ErrorCodes.AgentNotFound.Length > 0);
        AssertTrue(ErrorCodes.ConfigFileNotFound.Length > 0);
        AssertTrue(ErrorCodes.GeneralUnexpectedError.Length > 0);
    }

    private void TestFormatConsistency()
    {
        // All error codes should follow the pattern: CATEGORY_NNNN
        // Where CATEGORY is uppercase and NNNN is a 4-digit number
        
        var allCodes = GetAllErrorCodes();
        
        foreach (var code in allCodes)
        {
            // Should contain underscore
            AssertTrue(code.Contains('_'), $"Error code {code} should contain underscore");
            
            // Should have format CATEGORY_NNNN
            var parts = code.Split('_');
            AssertTrue(parts.Length == 2, $"Error code {code} should have format CATEGORY_NNNN");
            
            // Category should be uppercase
            AssertTrue(parts[0] == parts[0].ToUpperInvariant(), $"Error code {code} category should be uppercase");
            
            // Number part should be 4 digits
            AssertTrue(parts[1].Length == 4, $"Error code {code} number part should be 4 digits");
            AssertTrue(int.TryParse(parts[1], out _), $"Error code {code} number part should be numeric");
        }
    }

    private void TestUniqueness()
    {
        var allCodes = GetAllErrorCodes();
        var distinctCodes = allCodes.Distinct().ToList();
        
        AssertEqual(allCodes.Count, distinctCodes.Count, 
            $"Error codes should be unique. Found {allCodes.Count - distinctCodes.Count} duplicates");
        
        // Also verify no two codes have the same value
        var codeValues = allCodes.Select(c => c).ToList();
        var distinctValues = codeValues.Distinct().ToList();
        AssertEqual(codeValues.Count, distinctValues.Count, "All error code values should be unique");
    }

    private List<string> GetAllErrorCodes()
    {
        var errorCodesType = typeof(ErrorCodes);
        var fields = errorCodesType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        var codes = new List<string>();
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            if (value != null)
            {
                codes.Add(value);
            }
        }

        return codes;
    }
}

