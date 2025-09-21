using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Specs;
using Nexo.Core.Validation;
using Xunit;

namespace Nexo.Core.Tests.Validation
{
    public class FeatureSpecValidatorTests
    {
        private readonly Mock<ILogger<FeatureSpecValidator>> _mockLogger;
        private readonly FeatureSpecValidator _validator;

        public FeatureSpecValidatorTests()
        {
            _mockLogger = new Mock<ILogger<FeatureSpecValidator>>();
            _validator = new FeatureSpecValidator(_mockLogger.Object);
        }

        [Fact]
        public void ValidateFeatureSpec_WithValidSpec_ShouldPass()
        {
            // Arrange
            var validSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1", "capability2" },
                new Dictionary<string, string> { { "param1", "value1" } },
                new[] { "policy1", "policy2" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(validSpec);

            // Assert
            result.Passed.Should().BeTrue();
            result.Findings.Should().BeEmpty();
        }

        [Fact]
        public void ValidateFeatureSpec_WithEmptyName_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "",
                "1.0.0",
                new[] { "capability1" },
                new Dictionary<string, string>(),
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Name is required and cannot be empty");
        }

        [Fact]
        public void ValidateFeatureSpec_WithInvalidVersion_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "invalid-version",
                new[] { "capability1" },
                new Dictionary<string, string>(),
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Version 'invalid-version' is not a valid semantic version");
        }

        [Fact]
        public void ValidateFeatureSpec_WithEmptyCapabilities_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                Array.Empty<string>(),
                new Dictionary<string, string>(),
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("At least one capability is required");
        }

        [Fact]
        public void ValidateFeatureSpec_WithNullCapabilities_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                null!,
                new Dictionary<string, string>(),
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("At least one capability is required");
        }

        [Fact]
        public void ValidateFeatureSpec_WithEmptyCapability_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1", "" },
                new Dictionary<string, string>(),
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Capabilities cannot contain empty or null values");
        }

        [Fact]
        public void ValidateFeatureSpec_WithNullParameters_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1" },
                null!,
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Parameters cannot be null (use empty dictionary if no parameters)");
        }

        [Fact]
        public void ValidateFeatureSpec_WithEmptyParameterValue_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1" },
                new Dictionary<string, string> { { "param1", "" } },
                new[] { "policy1" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Parameter values cannot be empty or null");
        }

        [Fact]
        public void ValidateFeatureSpec_WithNullPolicies_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1" },
                new Dictionary<string, string>(),
                null!
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Policies cannot be null (use empty array if no policies)");
        }

        [Fact]
        public void ValidateFeatureSpec_WithEmptyPolicy_ShouldFail()
        {
            // Arrange
            var invalidSpec = new FeatureSpec(
                "Test Feature",
                "1.0.0",
                new[] { "capability1" },
                new Dictionary<string, string>(),
                new[] { "policy1", "" }
            );

            // Act
            var result = _validator.ValidateFeatureSpec(invalidSpec);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain("Policies cannot contain empty or null values");
        }

        [Fact]
        public void ValidateInput_WithValidJson_ShouldPass()
        {
            // Arrange
            var validJson = """
            {
                "name": "Test Feature",
                "version": "1.0.0",
                "capabilities": ["capability1", "capability2"],
                "parameters": {"param1": "value1"},
                "policies": ["policy1", "policy2"]
            }
            """;

            // Act
            var result = _validator.ValidateInput(validJson, false);

            // Assert
            result.Passed.Should().BeTrue();
            result.Findings.Should().BeEmpty();
        }

        [Fact]
        public void ValidateInput_WithInvalidJson_ShouldFail()
        {
            // Arrange
            var invalidJson = """
            {
                "name": "",
                "version": "invalid",
                "capabilities": [],
                "parameters": {},
                "policies": []
            }
            """;

            // Act
            var result = _validator.ValidateInput(invalidJson, false);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidateInput_WithValidYaml_ShouldPass()
        {
            // Arrange
            var validYaml = """
            name: Test Feature
            version: 1.0.0
            capabilities:
              - capability1
              - capability2
            parameters:
              param1: value1
            policies:
              - policy1
              - policy2
            """;

            // Act
            var result = _validator.ValidateInput(validYaml, true);

            // Assert
            result.Passed.Should().BeTrue();
            result.Findings.Should().BeEmpty();
        }

        [Fact]
        public void ValidateInput_WithInvalidYaml_ShouldFail()
        {
            // Arrange
            var invalidYaml = """
            name: ""
            version: invalid
            capabilities: []
            parameters: {}
            policies: []
            """;

            // Act
            var result = _validator.ValidateInput(invalidYaml, true);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidateInput_WithMalformedJson_ShouldFail()
        {
            // Arrange
            var malformedJson = "{ invalid json }";

            // Act
            var result = _validator.ValidateInput(malformedJson, false);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain(f => f.Contains("Failed to parse input"));
        }

        [Fact]
        public async Task ValidateFileAsync_WithValidFile_ShouldPass()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var validJson = """
            {
                "name": "Test Feature",
                "version": "1.0.0",
                "capabilities": ["capability1"],
                "parameters": {},
                "policies": []
            }
            """;
            await File.WriteAllTextAsync(tempFile, validJson);

            try
            {
                // Act
                var result = await _validator.ValidateFileAsync(tempFile);

                // Assert
                result.Passed.Should().BeTrue();
                result.Findings.Should().BeEmpty();
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ValidateFileAsync_WithNonExistentFile_ShouldFail()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent-file.json");

            // Act
            var result = await _validator.ValidateFileAsync(nonExistentFile);

            // Assert
            result.Passed.Should().BeFalse();
            result.Findings.Should().Contain($"File not found: {nonExistentFile}");
        }

        [Fact]
        public async Task ValidateAsync_WithValidJson_ShouldPass()
        {
            // Arrange
            var validJson = """
            {
                "name": "Test Feature",
                "version": "1.0.0",
                "capabilities": ["capability1"],
                "parameters": {},
                "policies": []
            }
            """;

            // Act
            var result = await _validator.ValidateAsync(validJson);

            // Assert
            result.Passed.Should().BeTrue();
            result.Findings.Should().BeEmpty();
        }
    }
}
