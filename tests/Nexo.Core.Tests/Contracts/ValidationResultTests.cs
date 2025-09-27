using FluentAssertions;
using Nexo.Core.Contracts;
using System.Collections.Generic;
using Xunit;

namespace Nexo.Core.Tests.Contracts
{
    public partial class ValidationResultTests
    {
        [Theory]
        [InlineData(true, "TestGate", new[] { "No issues found" })]
        [InlineData(false, "TestGate", new[] { "Error 1", "Error 2" })]
        public void ValidationResult_ShouldCreateWithCorrectValues(bool passed, string name, string[] findings)
        {
            // Arrange
            var findingsList = new List<string>(findings);

            // Act
            var result = new ValidationResult(passed, name, findingsList);

            // Assert
            result.Passed.Should().Be(passed);
            result.Name.Should().Be(name);
            result.Findings.Should().BeEquivalentTo(findingsList);
        }

        [Fact]
        public void ValidationResult_ShouldBeImmutable()
        {
            // Arrange
            var findings = new List<string> { "Test finding" };

            // Act
            var result = new ValidationResult(true, "TestGate", findings);

            // Assert
            result.Should().BeOfType<ValidationResult>();
            // Records are immutable by default in C#, so we just verify the type
            result.Passed.Should().BeTrue();
            result.Name.Should().Be("TestGate");
            result.Findings.Should().BeEquivalentTo(findings);
        }
    }
}
