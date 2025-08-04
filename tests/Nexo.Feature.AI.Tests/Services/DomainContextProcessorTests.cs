using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Xunit;
using System.Threading;

namespace Nexo.Feature.AI.Tests.Services
{
    /// <summary>
    /// Tests for DomainContextProcessor - Story 5.1.3: Domain Context Understanding.
    /// </summary>
    public class DomainContextProcessorTests
    {
        private readonly Mock<ILogger<DomainContextProcessor>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly DomainContextProcessor _processor;

        public DomainContextProcessorTests()
        {
            _mockLogger = new Mock<ILogger<DomainContextProcessor>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _processor = new DomainContextProcessor(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task ProcessDomainContextAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange
            var input = "Create a user registration system for an e-commerce platform";
            var domain = "E-commerce";
            var context = new DomainProcessingContext
            {
                Domain = domain,
                Industry = "Retail",
                BusinessContext = "Online shopping platform"
            };

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.ProcessedInput);
            Assert.Equal(domain, result.DomainContext.Domain);
            Assert.NotEmpty(result.Insights);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.NotEmpty(result.Recommendations);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task ProcessDomainContextAsync_WithEmptyInput_ReturnsFailureResult()
        {
            // Arrange
            var input = "";
            var domain = "E-commerce";
            var context = new DomainProcessingContext { Domain = domain };

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task ProcessDomainContextAsync_WithNullInput_ReturnsFailureResult()
        {
            // Arrange
            string input = null;
            var domain = "E-commerce";
            var context = new DomainProcessingContext { Domain = domain };

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task RecognizeBusinessTerminologyAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange
            var input = "Implement user authentication and data processing for healthcare records";
            var domain = "Healthcare";

            // Act
            var result = await _processor.RecognizeBusinessTerminologyAsync(input, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.RecognizedTerms);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.NotEmpty(result.Suggestions);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task RecognizeBusinessTerminologyAsync_WithEmptyInput_ReturnsFailureResult()
        {
            // Arrange
            var input = "";
            var domain = "Healthcare";

            // Act
            var result = await _processor.RecognizeBusinessTerminologyAsync(input, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task IdentifyIndustryPatternsAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange
            var input = "Build a secure payment processing system for financial transactions";
            var industry = "Finance";

            // Act
            var result = await _processor.IdentifyIndustryPatternsAsync(input, industry);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.IdentifiedPatterns);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.NotEmpty(result.Recommendations);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task IdentifyIndustryPatternsAsync_WithEmptyInput_ReturnsFailureResult()
        {
            // Arrange
            var input = "";
            var industry = "Finance";

            // Act
            var result = await _processor.IdentifyIndustryPatternsAsync(input, industry);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task IntegrateDomainKnowledgeAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange
            var input = "Design a patient management system for healthcare providers";
            var domain = "Healthcare";

            // Act
            var result = await _processor.IntegrateDomainKnowledgeAsync(input, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.EnhancedInput);
            Assert.NotEmpty(result.AppliedKnowledge);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task IntegrateDomainKnowledgeAsync_WithEmptyInput_ReturnsFailureResult()
        {
            // Arrange
            var input = "";
            var domain = "Healthcare";

            // Act
            var result = await _processor.IntegrateDomainKnowledgeAsync(input, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public void GetSupportedDomains_ReturnsExpectedDomains()
        {
            // Act
            var domains = _processor.GetSupportedDomains().ToList();

            // Assert
            Assert.NotEmpty(domains);
            Assert.Contains("E-commerce", domains);
            Assert.Contains("Healthcare", domains);
            Assert.Contains("Finance", domains);
            Assert.Contains("Education", domains);
            Assert.Contains("Technology", domains);
        }

        [Fact]
        public void GetSupportedIndustries_ReturnsExpectedIndustries()
        {
            // Act
            var industries = _processor.GetSupportedIndustries().ToList();

            // Assert
            Assert.NotEmpty(industries);
            Assert.Contains("Technology", industries);
            Assert.Contains("Healthcare", industries);
            Assert.Contains("Finance", industries);
            Assert.Contains("Education", industries);
            Assert.Contains("Manufacturing", industries);
        }

        [Fact]
        public async Task ValidateDomainRequirementsAsync_WithValidRequirements_ReturnsSuccessResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "User Authentication",
                    Description = "Implement secure user authentication system",
                    Type = RequirementType.Security,
                    Priority = RequirementPriority.High
                },
                new FeatureRequirement
                {
                    Id = "req2",
                    Title = "Data Processing",
                    Description = "Process user data according to business rules",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.Medium
                }
            };
            var domain = "E-commerce";

            // Act
            var result = await _processor.ValidateDomainRequirementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.IsValid);
            Assert.True(result.ValidationScore > 0.0);
            Assert.NotEmpty(result.Recommendations);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task ValidateDomainRequirementsAsync_WithInvalidRequirements_ReturnsIssues()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "Invalid Requirement",
                    Description = "", // Empty description should trigger validation issue
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High
                }
            };
            var domain = "E-commerce";

            // Act
            var result = await _processor.ValidateDomainRequirementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Issues);
            Assert.True(result.ValidationScore < 1.0);
        }

        [Fact]
        public async Task ValidateDomainRequirementsAsync_WithEmptyRequirements_ReturnsSuccessResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>();
            var domain = "E-commerce";

            // Act
            var result = await _processor.ValidateDomainRequirementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.IsValid);
            Assert.Equal(0.0, result.ValidationScore);
        }

        [Fact]
        public async Task SuggestDomainImprovementsAsync_WithValidRequirements_ReturnsSuccessResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "User Management",
                    Description = "Manage user accounts and profiles",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High
                }
            };
            var domain = "E-commerce";

            // Act
            var result = await _processor.SuggestDomainImprovementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Improvements);
            Assert.True(result.ImprovementScore > 0.0);
            Assert.NotEmpty(result.BestPractices);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task SuggestDomainImprovementsAsync_WithEmptyRequirements_ReturnsSuccessResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>();
            var domain = "E-commerce";

            // Act
            var result = await _processor.SuggestDomainImprovementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Improvements);
            Assert.Equal(0.0, result.ImprovementScore);
        }

        [Theory]
        [InlineData("E-commerce")]
        [InlineData("Healthcare")]
        [InlineData("Finance")]
        [InlineData("Education")]
        public async Task ProcessDomainContextAsync_WithDifferentDomains_ReturnsDomainSpecificResults(string domain)
        {
            // Arrange
            var input = "Create a secure data processing system";
            var context = new DomainProcessingContext { Domain = domain };

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(domain, result.DomainContext.Domain);
            Assert.True(result.ConfidenceScore > 0.0);
        }

        [Theory]
        [InlineData("Technology")]
        [InlineData("Healthcare")]
        [InlineData("Finance")]
        public async Task IdentifyIndustryPatternsAsync_WithDifferentIndustries_ReturnsIndustrySpecificResults(string industry)
        {
            // Arrange
            var input = "Implement secure authentication and authorization";

            // Act
            var result = await _processor.IdentifyIndustryPatternsAsync(input, industry);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.NotEmpty(result.Recommendations);
        }

        [Fact]
        public async Task ProcessDomainContextAsync_WithComplexInput_ReturnsComprehensiveResults()
        {
            // Arrange
            var input = @"Create a comprehensive e-commerce platform with the following features:
1. User registration and authentication with multi-factor authentication
2. Product catalog with search and filtering capabilities
3. Shopping cart and checkout process with secure payment processing
4. Order management and tracking system
5. Customer support and feedback system
6. Admin dashboard for inventory and user management
7. Mobile-responsive design for all devices
8. Integration with third-party payment gateways
9. Real-time inventory tracking
10. Customer analytics and reporting";

            var domain = "E-commerce";
            var context = new DomainProcessingContext
            {
                Domain = domain,
                Industry = "Retail",
                BusinessContext = "Online marketplace for various products"
            };

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.ProcessedInput);
            Assert.Equal(domain, result.DomainContext.Domain);
            Assert.NotEmpty(result.Insights);
            Assert.True(result.ConfidenceScore > 0.7); // Should have high confidence for detailed input
            Assert.NotEmpty(result.Recommendations);
            Assert.NotEmpty(result.DomainContext.Stakeholders);
            Assert.NotEmpty(result.DomainContext.ComplianceRequirements);
            Assert.NotEmpty(result.DomainContext.BusinessProcesses);
            Assert.NotEmpty(result.DomainContext.TechnicalConstraints);
            Assert.NotEmpty(result.DomainContext.DomainRules);
        }

        [Fact]
        public async Task RecognizeBusinessTerminologyAsync_WithHealthcareInput_ReturnsHealthcareTerms()
        {
            // Arrange
            var input = "Implement HIPAA-compliant patient data management with electronic health records (EHR) integration, clinical decision support, and telemedicine capabilities for healthcare providers";

            var domain = "Healthcare";

            // Act
            var result = await _processor.RecognizeBusinessTerminologyAsync(input, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.RecognizedTerms);
            Assert.True(result.ConfidenceScore > 0.0);
            
            // Verify that healthcare-specific terms are recognized
            var terms = result.RecognizedTerms.Select(t => t.Term.ToLower()).ToList();
            Assert.Contains("user registration", terms);
            Assert.Contains("data processing", terms);
        }

        [Fact]
        public async Task IntegrateDomainKnowledgeAsync_WithFinanceInput_ReturnsFinanceKnowledge()
        {
            // Arrange
            var input = "Design a secure banking system with transaction processing, fraud detection, and regulatory compliance for financial institutions";

            var domain = "Finance";

            // Act
            var result = await _processor.IntegrateDomainKnowledgeAsync(input, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.EnhancedInput);
            Assert.NotEmpty(result.AppliedKnowledge);
            Assert.True(result.ConfidenceScore > 0.0);
            Assert.NotEmpty(result.KnowledgeGaps);
        }

        [Fact]
        public async Task ValidateDomainRequirementsAsync_WithMultipleRequirements_ValidatesAllRequirements()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "Valid Requirement 1",
                    Description = "This is a valid requirement with proper description",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High
                },
                new FeatureRequirement
                {
                    Id = "req2",
                    Title = "Valid Requirement 2",
                    Description = "Another valid requirement with good description",
                    Type = RequirementType.Security,
                    Priority = RequirementPriority.Critical
                },
                new FeatureRequirement
                {
                    Id = "req3",
                    Title = "Invalid Requirement",
                    Description = "", // Empty description
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.Medium
                }
            };
            var domain = "E-commerce";

            // Act
            var result = await _processor.ValidateDomainRequirementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Issues);
            Assert.True(result.ValidationScore > 0.0 && result.ValidationScore < 1.0);
            Assert.NotEmpty(result.Recommendations);
        }

        [Fact]
        public async Task SuggestDomainImprovementsAsync_WithMultipleRequirements_ProvidesComprehensiveImprovements()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "User Authentication",
                    Description = "Implement user authentication",
                    Type = RequirementType.Security,
                    Priority = RequirementPriority.High
                },
                new FeatureRequirement
                {
                    Id = "req2",
                    Title = "Data Processing",
                    Description = "Process user data",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.Medium
                },
                new FeatureRequirement
                {
                    Id = "req3",
                    Title = "Performance Optimization",
                    Description = "Optimize system performance",
                    Type = RequirementType.Performance,
                    Priority = RequirementPriority.Low
                }
            };
            var domain = "E-commerce";

            // Act
            var result = await _processor.SuggestDomainImprovementsAsync(requirements, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Improvements);
            Assert.True(result.ImprovementScore > 0.0);
            Assert.NotEmpty(result.BestPractices);
            
            // Verify improvements are provided for different requirement types
            var improvementTypes = result.Improvements.Select(i => i.Type).Distinct().ToList();
            Assert.Contains(ImprovementType.Terminology, improvementTypes);
        }

        [Fact]
        public async Task ProcessDomainContextAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var input = "Test input";
            var domain = "E-commerce";
            var context = new DomainProcessingContext { Domain = domain };

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.ProcessDomainContextAsync(input, domain, context);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
            Assert.Equal("DomainContextProcessor", result.Metadata.ProcessingModel);
        }

        [Fact]
        public async Task RecognizeBusinessTerminologyAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var input = "Test input";
            var domain = "E-commerce";

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.RecognizeBusinessTerminologyAsync(input, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task IdentifyIndustryPatternsAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var input = "Test input";
            var industry = "Technology";

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.IdentifyIndustryPatternsAsync(input, industry);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task IntegrateDomainKnowledgeAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var input = "Test input";
            var domain = "E-commerce";

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.IntegrateDomainKnowledgeAsync(input, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task ValidateDomainRequirementsAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "Test Requirement",
                    Description = "Test description",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High
                }
            };
            var domain = "E-commerce";

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.ValidateDomainRequirementsAsync(requirements, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ValidationScore);
        }

        [Fact]
        public async Task SuggestDomainImprovementsAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Id = "req1",
                    Title = "Test Requirement",
                    Description = "Test description",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High
                }
            };
            var domain = "E-commerce";

            // Mock the model orchestrator to throw an exception
            _mockModelOrchestrator.Setup(m => m.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _processor.SuggestDomainImprovementsAsync(requirements, domain);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0.0, result.ImprovementScore);
        }
    }
}