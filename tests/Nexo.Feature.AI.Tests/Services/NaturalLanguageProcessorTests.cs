using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;

namespace Nexo.Feature.AI.Tests.Services
{
    public class NaturalLanguageProcessorTests
    {
        private NaturalLanguageProcessor _processor;
        private Mock<ILogger<NaturalLanguageProcessor>> _mockLogger;
        private Mock<IModelOrchestrator> _mockModelOrchestrator;

        public NaturalLanguageProcessorTests()
        {
            _mockLogger = new Mock<ILogger<NaturalLanguageProcessor>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            
            // Setup the mock to return a valid response
            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<ModelRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModelResponse
                {
                    Content = "Extracted requirement: Test feature",
                    Model = "test-model",
                    ProcessingTimeMs = 50
                });
            
            _processor = new NaturalLanguageProcessor(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithValidInput_ShouldReturnSuccessResult()
        {
            // Arrange
            var input = "Create a user authentication system";
            var context = new ProcessingContext
            {
                Domain = "E-commerce",
                BusinessRules = new List<string> { "All user data must be encrypted" },
                UserRoles = new List<string> { "Customer", "Admin" },
                QualityAttributes = new QualityAttributes
                {
                    Security = "High security requirements",
                    Performance = "Response time under 2 seconds",
                    Usability = "Intuitive user interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Requirements);
            Assert.True(result.ConfidenceScore > 0);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithEmptyInput_ShouldReturnFailureResult()
        {
            // Arrange
            var input = "";
            var context = new ProcessingContext { Domain = "Test" };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input is empty.", result.Errors);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithNullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            string input = null;
            var context = new ProcessingContext { Domain = "Test" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _processor.ProcessRequirementsAsync(input, context));
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var input = "Test input";
            ProcessingContext context = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _processor.ProcessRequirementsAsync(input, context));
        }

        [Fact]
        public async Task ValidateInputAsync_WithValidInput_ShouldReturnValidResult()
        {
            // Arrange
            var input = "Valid input";
            var context = new ValidationContext 
            { 
                RequiredFields = new List<string> { "title", "description" },
                BusinessRules = new List<string> { "Must be clear and concise" }
            };

            // Act
            var result = await _processor.ValidateInputAsync(input, context);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Issues);
        }

        [Fact]
        public async Task ExtractComponentsAsync_WithValidInput_ShouldReturnSuccessResult()
        {
            // Arrange
            var input = "Extract components from this text";
            var extractionType = ExtractionType.UserStories;
            var context = new ExtractionContext { Domain = "Test" };

            // Act
            var result = await _processor.ExtractComponentsAsync(input, extractionType, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Components);
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task ProcessDomainTerminologyAsync_WithValidInput_ShouldReturnSuccessResult()
        {
            // Arrange
            var input = "Process domain terminology";
            var domain = "Healthcare";

            // Act
            var result = await _processor.ProcessDomainTerminologyAsync(input, domain);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.RecognizedTerms);
            Assert.NotNull(result.UnrecognizedTerms);
        }

        [Fact]
        public async Task CategorizeAndPrioritizeAsync_WithValidRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Title = "User Authentication",
                    Description = "Implement user login functionality",
                    Priority = RequirementPriority.High
                }
            };
            var context = new PrioritizationContext 
            { 
                BusinessValueWeights = new Dictionary<string, double> { { "authentication", 0.8 } },
                TechnicalComplexityWeights = new Dictionary<string, double> { { "authentication", 0.6 } }
            };

            // Act
            var result = await _processor.CategorizeAndPrioritizeAsync(requirements, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.PrioritizedRequirements);
            Assert.NotNull(result.Metrics);
        }

        [Fact]
        public void SupportsFormat_WithPlainText_ShouldReturnTrue()
        {
            // Act
            var result = _processor.SupportsFormat(InputFormat.PlainText);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetSupportedFormats_ShouldReturnExpectedFormats()
        {
            // Act
            var formats = _processor.GetSupportedFormats();

            // Assert
            Assert.Contains(InputFormat.PlainText, formats);
        }

        [Fact]
        public void GetSupportedDomains_ShouldReturnExpectedDomains()
        {
            // Act
            var domains = _processor.GetSupportedDomains();

            // Assert
            Assert.Contains("General", domains);
            Assert.Contains("E-commerce", domains);
            Assert.Contains("Healthcare", domains);
            Assert.Contains("Finance", domains);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithComplexInput_ShouldHandleMultipleRequirements()
        {
            // Arrange
            var input = "Create a user authentication system that allows users to log in with email and password. Also implement password reset functionality.";
            var context = new ProcessingContext
            {
                Domain = "E-commerce",
                BusinessRules = new List<string> { "All user data must be encrypted", "Passwords must be at least 8 characters" },
                UserRoles = new List<string> { "Customer", "Admin" },
                QualityAttributes = new QualityAttributes
                {
                    Security = "High security requirements",
                    Performance = "Response time under 2 seconds",
                    Usability = "Intuitive user interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Requirements);
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithBusinessRules_ShouldApplyContext()
        {
            // Arrange
            var input = "Implement user registration";
            var context = new ProcessingContext
            {
                Domain = "Healthcare",
                BusinessRules = new List<string> 
                { 
                    "HIPAA compliance required",
                    "Two-factor authentication mandatory",
                    "Audit logging required"
                },
                UserRoles = new List<string> { "Patient", "Doctor", "Admin" },
                QualityAttributes = new QualityAttributes
                {
                    Security = "Maximum security requirements",
                    Performance = "Fast response times",
                    Usability = "Accessible interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Healthcare", result.Metadata.Domain);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithQualityAttributes_ShouldConsiderNonFunctionalRequirements()
        {
            // Arrange
            var input = "Build a real-time chat system";
            var context = new ProcessingContext
            {
                Domain = "Communication",
                QualityAttributes = new QualityAttributes
                {
                    Performance = "Real-time performance required",
                    Scalability = "Must scale to thousands of users",
                    Usability = "Simple and intuitive interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithUserRoles_ShouldConsiderAccessControl()
        {
            // Arrange
            var input = "Create a document management system";
            var context = new ProcessingContext
            {
                Domain = "Enterprise",
                UserRoles = new List<string> 
                { 
                    "Viewer", 
                    "Editor", 
                    "Manager", 
                    "Administrator" 
                },
                QualityAttributes = new QualityAttributes
                {
                    Security = "Role-based access control",
                    Usability = "Easy to use interface",
                    Performance = "Fast document retrieval"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Requirements);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithEnhancedExtraction_ShouldValidateAndCompleteRequirements()
        {
            // Arrange
            var input = "Create a user authentication system that allows users to log in with email and password";
            var context = new ProcessingContext
            {
                Domain = "E-commerce",
                BusinessRules = new List<string> { "All user data must be encrypted", "Passwords must be at least 8 characters" },
                UserRoles = new List<string> { "Customer", "Admin" },
                QualityAttributes = new QualityAttributes
                {
                    Security = "High security requirements",
                    Performance = "Response time under 2 seconds",
                    Usability = "Intuitive user interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Requirements);
            Assert.True(result.ConfidenceScore > 0);
            
            // Verify that requirements have been processed with enhanced extraction
            foreach (var requirement in result.Requirements)
            {
                Assert.NotNull(requirement.Title);
                Assert.NotNull(requirement.Description);
                Assert.True(requirement.CompletenessScore >= 0 && requirement.CompletenessScore <= 1);
            }
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithDomainSpecificTerminology_ShouldRecognizeTerms()
        {
            // Arrange
            var input = "Implement EHR system with HL7 integration and FHIR compliance";
            var context = new ProcessingContext
            {
                Domain = "Healthcare",
                BusinessRules = new List<string> { "HIPAA compliance required" },
                UserRoles = new List<string> { "Doctor", "Nurse", "Admin" },
                QualityAttributes = new QualityAttributes
                {
                    Security = "HIPAA compliant security",
                    Performance = "Fast medical record access",
                    Usability = "Medical professional interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Healthcare", result.Metadata.Domain);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithPerformanceRequirements_ShouldConsiderScalability()
        {
            // Arrange
            var input = "Build a high-traffic e-commerce platform";
            var context = new ProcessingContext
            {
                Domain = "E-commerce",
                QualityAttributes = new QualityAttributes
                {
                    Performance = "Handle thousands of concurrent users",
                    Scalability = "Auto-scaling infrastructure",
                    Security = "Secure payment processing"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithSecurityFocus_ShouldPrioritizeSecurityRequirements()
        {
            // Arrange
            var input = "Create a financial transaction processing system";
            var context = new ProcessingContext
            {
                Domain = "Finance",
                BusinessRules = new List<string> 
                { 
                    "PCI DSS compliance required",
                    "End-to-end encryption mandatory",
                    "Multi-factor authentication required"
                },
                QualityAttributes = new QualityAttributes
                {
                    Security = "Maximum security for financial data",
                    Performance = "Fast transaction processing",
                    Usability = "Secure but user-friendly interface"
                }
            };

            // Act
            var result = await _processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Finance", result.Metadata.Domain);
        }
    }
}