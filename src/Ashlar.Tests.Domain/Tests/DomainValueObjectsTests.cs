using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Values;

namespace Ashlar.Tests.Domain.Tests;

/// <summary>
/// Tests for domain value objects.
/// </summary>
public class DomainValueObjectsTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test RiskLevel value object
            var low = RiskLevel.Low;
            var medium = RiskLevel.Medium;
            var high = RiskLevel.High;
            var critical = RiskLevel.Critical;

            // Test static instances exist
            AssertNotNull(low);
            AssertNotNull(medium);
            AssertNotNull(high);
            AssertNotNull(critical);

            // Test properties
            AssertEqual("Low", low.Name);
            AssertEqual(1, low.Value);
            AssertEqual("Medium", medium.Name);
            AssertEqual(2, medium.Value);
            AssertEqual("High", high.Name);
            AssertEqual(3, high.Value);
            AssertEqual("Critical", critical.Name);
            AssertEqual(4, critical.Value);

            // Test equality
            AssertTrue(low == RiskLevel.Low);
            AssertTrue(low != medium);
            AssertTrue(low.Equals(RiskLevel.Low));

            // Test factory methods
            AssertEqual(low, RiskLevel.FromName("Low"));
            AssertEqual(low, RiskLevel.FromValue(1));
            AssertEqual(medium, RiskLevel.FromName("Medium"));
            AssertEqual(medium, RiskLevel.FromValue(2));

            // Test All collection
            AssertEqual(4, RiskLevel.All.Count);
            AssertTrue(RiskLevel.All.Contains(low));
            AssertTrue(RiskLevel.All.Contains(medium));
            AssertTrue(RiskLevel.All.Contains(high));
            AssertTrue(RiskLevel.All.Contains(critical));

            // Test ToString
            AssertEqual("Low", low.ToString());
            AssertEqual("Medium", medium.ToString());

            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainValueObjectsTests),
                Category = "Domain",
                Passed = true,
                Message = "All domain value object tests passed"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(DomainValueObjectsTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}

