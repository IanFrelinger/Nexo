using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Nexo.Feature.AI.Enums;
using Xunit;
using System.Threading;

namespace Nexo.Feature.AI.Tests.Services
{
    /// <summary>
    /// Tests for the DomainLogicGenerator service
    /// </summary>
    public class DomainLogicGeneratorTests
    {
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly Mock<ILogger<DomainLogicGenerator>> _mockLogger;
        private readonly DomainLogicGenerator _domainLogicGenerator;

        public DomainLogicGeneratorTests()
        {
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _mockLogger = new Mock<ILogger<DomainLogicGenerator>>();
            _domainLogicGenerator = new DomainLogicGenerator(_mockModelOrchestrator.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            var instance = new DomainLogicGenerator(_mockModelOrchestrator.Object, _mockLogger.Object);
            Assert.NotNull(instance);
        }

        [Fact]
        public void Constructor_WithNullModelOrchestrator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainLogicGenerator(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainLogicGenerator(_mockModelOrchestrator.Object, null!));
        }

        [Fact]
        public async Task GenerateDomainLogicAsync_WithValidRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            var mockResponse = new ModelResponse
            {
                Content = "{\"businessRules\": [{\"name\": \"UserValidation\", \"description\": \"Validate user input\"}]}"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _domainLogicGenerator.GenerateDomainLogicAsync(requirements, domainContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedLogic);
            Assert.NotNull(result.GeneratedLogic.Entities);
            Assert.NotNull(result.GeneratedLogic.BusinessRules);
        }

        [Fact]
        public async Task GenerateDomainLogicAsync_WithEmptyRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            var mockResponse = new ModelResponse
            {
                Content = "{\"businessRules\": []}"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _domainLogicGenerator.GenerateDomainLogicAsync(requirements, domainContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedLogic);
        }

        [Fact]
        public async Task GenerateDomainLogicAsync_WhenModelOrchestratorFails_ShouldReturnFailureResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ModelResponse?)null);

            // Act
            var result = await _domainLogicGenerator.GenerateDomainLogicAsync(requirements, domainContext);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ExtractBusinessRulesAsync_WithValidRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            var mockResponse = new ModelResponse
            {
                Content = "{\"businessRules\": [{\"name\": \"UserValidation\", \"description\": \"Validate user input\"}]}"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _domainLogicGenerator.ExtractBusinessRulesAsync(requirements, domainContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.ExtractedRules);
        }

        [Fact]
        public async Task GenerateDomainEntitiesAsync_WithValidRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            var businessRules = new BusinessRuleExtractionResult
            {
                IsSuccess = true,
                ExtractedRules = new List<BusinessRule>
                {
                    new BusinessRule
                    {
                        Name = "UserValidation",
                        Description = "Validate user input"
                    }
                }
            };

            var mockResponse = new ModelResponse
            {
                Content = "{\"entities\": [{\"name\": \"User\", \"description\": \"User entity\"}]}"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _domainLogicGenerator.GenerateDomainEntitiesAsync(requirements, businessRules);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedEntities);
        }

        [Fact]
        public async Task GenerateValueObjectsAsync_WithValidRequirements_ShouldReturnSuccessResult()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            var businessRules = new BusinessRuleExtractionResult
            {
                IsSuccess = true,
                ExtractedRules = new List<BusinessRule>
                {
                    new BusinessRule
                    {
                        Name = "UserValidation",
                        Description = "Validate user input"
                    }
                }
            };

            var mockResponse = new ModelResponse
            {
                Content = "{\"valueObjects\": [{\"name\": \"Email\", \"description\": \"Email value object\"}]}"
            };

            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _domainLogicGenerator.GenerateValueObjectsAsync(requirements, businessRules);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GeneratedValueObjects);
        }

        [Fact]
        public async Task ValidateDomainLogicAsync_WithValidDomainLogic_ShouldReturnValidationResult()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic
                {
                    Entities = new List<DomainEntity>
                    {
                        new DomainEntity
                        {
                            Name = "User",
                            Description = "User entity",
                            Type = EntityType.Core
                        }
                    },
                    BusinessRules = new List<BusinessRule>
                    {
                        new BusinessRule
                        {
                            Name = "UserValidation",
                            Description = "Validate user input"
                        }
                    }
                }
            };

            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>
                {
                    new FeatureRequirement
                    {
                        Title = "User Management",
                        Description = "System should allow user registration and login"
                    }
                }
            };

            // Act
            var result = await _domainLogicGenerator.ValidateDomainLogicAsync(domainLogic, requirements);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.NotNull(result.Issues);
        }

        [Fact]
        public async Task OptimizeDomainLogicAsync_WithValidDomainLogic_ShouldReturnOptimizationResult()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic
                {
                    Entities = new List<DomainEntity>
                    {
                        new DomainEntity
                        {
                            Name = "User",
                            Description = "User entity",
                            Type = EntityType.Core
                        }
                    }
                }
            };

            var optimizationOptions = new DomainLogicOptimizationOptions
            {
                OptimizePerformance = true,
                OptimizeMaintainability = true,
                OptimizeReadability = true,
                ApplyDesignPatterns = true,
                OptimizeNamingConventions = true
            };

            // Act
            var result = await _domainLogicGenerator.OptimizeDomainLogicAsync(domainLogic, optimizationOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Suggestions);
        }

        [Fact]
        public async Task GenerateDomainLogicAsync_WithNullRequirements_ShouldThrowArgumentNullException()
        {
            // Arrange
            ProcessedRequirements? requirements = null;
            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateDomainLogicAsync(requirements!, domainContext));
        }

        [Fact]
        public async Task GenerateDomainLogicAsync_WithNullDomainContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            DomainContext? domainContext = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateDomainLogicAsync(requirements, domainContext!));
        }

        [Fact]
        public async Task ExtractBusinessRulesAsync_WithNullRequirements_ShouldThrowArgumentNullException()
        {
            // Arrange
            ProcessedRequirements? requirements = null;
            var domainContext = new DomainContext
            {
                Domain = "E-commerce",
                Industry = "Retail"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.ExtractBusinessRulesAsync(requirements!, domainContext));
        }

        [Fact]
        public async Task ExtractBusinessRulesAsync_WithNullDomainContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            DomainContext? domainContext = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.ExtractBusinessRulesAsync(requirements, domainContext!));
        }

        [Fact]
        public async Task GenerateDomainEntitiesAsync_WithNullRequirements_ShouldThrowArgumentNullException()
        {
            // Arrange
            ProcessedRequirements? requirements = null;
            var businessRules = new BusinessRuleExtractionResult
            {
                IsSuccess = true,
                ExtractedRules = new List<BusinessRule>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateDomainEntitiesAsync(requirements!, businessRules));
        }

        [Fact]
        public async Task GenerateDomainEntitiesAsync_WithNullBusinessRules_ShouldThrowArgumentNullException()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            BusinessRuleExtractionResult? businessRules = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateDomainEntitiesAsync(requirements, businessRules!));
        }

        [Fact]
        public async Task GenerateValueObjectsAsync_WithNullRequirements_ShouldThrowArgumentNullException()
        {
            // Arrange
            ProcessedRequirements? requirements = null;
            var businessRules = new BusinessRuleExtractionResult
            {
                IsSuccess = true,
                ExtractedRules = new List<BusinessRule>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateValueObjectsAsync(requirements!, businessRules));
        }

        [Fact]
        public async Task GenerateValueObjectsAsync_WithNullBusinessRules_ShouldThrowArgumentNullException()
        {
            // Arrange
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            BusinessRuleExtractionResult? businessRules = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.GenerateValueObjectsAsync(requirements, businessRules!));
        }

        [Fact]
        public async Task ValidateDomainLogicAsync_WithNullDomainLogic_ShouldThrowArgumentNullException()
        {
            // Arrange
            DomainLogicResult? domainLogic = null;
            var requirements = new ProcessedRequirements
            {
                Requirements = new List<FeatureRequirement>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.ValidateDomainLogicAsync(domainLogic!, requirements));
        }

        [Fact]
        public async Task ValidateDomainLogicAsync_WithNullRequirements_ShouldThrowArgumentNullException()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            ProcessedRequirements? requirements = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.ValidateDomainLogicAsync(domainLogic, requirements!));
        }

        [Fact]
        public async Task OptimizeDomainLogicAsync_WithNullDomainLogic_ShouldThrowArgumentNullException()
        {
            // Arrange
            DomainLogicResult? domainLogic = null;
            var optimizationOptions = new DomainLogicOptimizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.OptimizeDomainLogicAsync(domainLogic!, optimizationOptions));
        }

        [Fact]
        public async Task OptimizeDomainLogicAsync_WithNullOptimizationOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            DomainLogicOptimizationOptions? optimizationOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _domainLogicGenerator.OptimizeDomainLogicAsync(domainLogic, optimizationOptions!));
        }
    }
}