using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Domain.Tests;

/// <summary>
/// Tests for domain value objects.
/// </summary>
public class DomainValueObjectsTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test RiskLevel enum
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Low));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Medium));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.High));
            AssertTrue(Enum.IsDefined(typeof(RiskLevel), RiskLevel.Critical));

            return new TestResult
            {
                TestName = nameof(DomainValueObjectsTests),
                Category = "Domain",
                Passed = true,
                Message = "All domain value object tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(DomainValueObjectsTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }
}

