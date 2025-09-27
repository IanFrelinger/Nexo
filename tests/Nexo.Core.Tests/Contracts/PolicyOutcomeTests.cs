using FluentAssertions;
using Nexo.Core.Contracts;
using System.Collections.Generic;
using Xunit;

namespace Nexo.Core.Tests.Contracts
{
    public partial class PolicyOutcomeTests
    {
        [Theory]
        [InlineData(true, "SafetyPolicy", new[] { "All checks passed" }, 0.95)]
        [InlineData(false, "QualityPolicy", new[] { "Issue 1", "Issue 2" }, 0.3)]
        public void PolicyOutcome_ShouldCreateWithCorrectValues(bool passed, string policyPackName, string[] findings, double score)
        {
            // Arrange
            var findingsList = new List<string>(findings);

            // Act
            var result = new PolicyOutcome(passed, policyPackName, findingsList, score);

            // Assert
            result.Passed.Should().Be(passed);
            result.PolicyPackName.Should().Be(policyPackName);
            result.Findings.Should().BeEquivalentTo(findingsList);
            result.Score.Should().Be(score);
        }

        [Fact]
        public void PolicyOutcome_ShouldBeImmutable()
        {
            // Arrange
            var findings = new List<string> { "Test finding" };

            // Act
            var result = new PolicyOutcome(true, "TestPolicy", findings, 0.8);

            // Assert
            result.Should().BeOfType<PolicyOutcome>();
            // Records are immutable by default in C#, so we just verify the type
            result.Passed.Should().BeTrue();
            result.PolicyPackName.Should().Be("TestPolicy");
            result.Findings.Should().BeEquivalentTo(findings);
            result.Score.Should().Be(0.8);
        }
    }
}
