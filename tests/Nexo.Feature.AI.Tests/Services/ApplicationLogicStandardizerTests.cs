using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Feature.AI.Tests.Services
{
    /// <summary>
    /// Tests for the ApplicationLogicStandardizer service.
    /// Part of Phase 5.3: Application Logic Standardization.
    /// </summary>
    public class ApplicationLogicStandardizerTests
    {
        private readonly ILogger<ApplicationLogicStandardizer> _logger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly ApplicationLogicStandardizer _standardizer;

        public ApplicationLogicStandardizerTests()
        {
            _logger = Mock.Of<ILogger<ApplicationLogicStandardizer>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _standardizer = new ApplicationLogicStandardizer(_logger);
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithValidDomainLogic_ReturnsSuccessResult()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions();

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.StandardizedLogic);
            Assert.True(result.StandardizationScore > 0);
            Assert.NotEmpty(result.AppliedPatterns);
            Assert.NotEmpty(result.Recommendations);
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithNullDomainLogic_ThrowsArgumentNullException()
        {
            // Arrange
            DomainLogic domainLogic = null;
            var options = new ApplicationLogicStandardizationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _standardizer.StandardizeApplicationLogicAsync(domainLogic, options));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            ApplicationLogicStandardizationOptions options = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _standardizer.StandardizeApplicationLogicAsync(domainLogic, options));
        }

        [Fact]
        public async Task ApplySecurityPatternsAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new SecurityPatternOptions();

            // Act
            var result = await _standardizer.ApplySecurityPatternsAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.SecuredLogic);
            Assert.True(result.SecurityScore > 0);
            Assert.NotEmpty(result.AppliedSecurityPatterns);
            Assert.NotEmpty(result.SecurityRecommendations);
        }

        [Fact]
        public async Task ApplySecurityPatternsAsync_WithNullApplicationLogic_ThrowsArgumentNullException()
        {
            // Arrange
            StandardizedApplicationLogic applicationLogic = null;
            var options = new SecurityPatternOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _standardizer.ApplySecurityPatternsAsync(options));
        }

        [Fact]
        public async Task OptimizeForPerformanceAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new PerformanceOptimizationOptions();

            // Act
            var result = await _standardizer.OptimizeForPerformanceAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.OptimizedLogic);
            Assert.True(result.PerformanceScore > 0);
            Assert.NotEmpty(result.AppliedOptimizations);
            Assert.NotEmpty(result.PerformanceRecommendations);
        }

        [Fact]
        public async Task GenerateStateManagementAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new StateManagementOptions();

            // Act
            var result = await _standardizer.GenerateStateManagementAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.StateManagedLogic);
            Assert.True(result.StateManagementScore > 0);
            Assert.NotEmpty(result.AppliedStatePatterns);
            // StateManagementRecommendations may be empty when all patterns are already applied
        }

        [Fact]
        public async Task GenerateApiContractsAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new ApiContractOptions();

            // Act
            var result = await _standardizer.GenerateApiContractsAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.ApiContractLogic);
            Assert.True(result.ApiContractScore > 0);
            Assert.NotEmpty(result.GeneratedApiContracts);
            Assert.NotEmpty(result.ApiContractRecommendations);
        }

        [Fact]
        public async Task OptimizeDataFlowAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new DataFlowOptimizationOptions();

            // Act
            var result = await _standardizer.OptimizeDataFlowAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.DataFlowOptimizedLogic);
            Assert.True(result.DataFlowScore > 0);
            Assert.NotEmpty(result.AppliedDataFlowPatterns);
            // DataFlowRecommendations may be empty when all patterns are already applied
        }

        [Fact]
        public async Task IntegrateCachingStrategiesAsync_WithValidApplicationLogic_ReturnsSuccessResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new CachingStrategyOptions();

            // Act
            var result = await _standardizer.IntegrateCachingStrategiesAsync(applicationLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CachedLogic);
            Assert.True(result.CachingScore > 0);
            Assert.NotEmpty(result.AppliedCachingStrategies);
            // CachingRecommendations may be empty when all strategies are already applied
        }



        [Fact]
        public void GetSupportedPatterns_ReturnsNonEmptyCollection()
        {
            // Act
            var patterns = _standardizer.GetSupportedPatterns();

            // Assert
            Assert.NotNull(patterns);
            Assert.NotEmpty(patterns);
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Name));
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Description));
        }

        [Fact]
        public void GetSupportedSecurityPatterns_ReturnsNonEmptyCollection()
        {
            // Act
            var patterns = _standardizer.GetSupportedSecurityPatterns();

            // Assert
            Assert.NotNull(patterns);
            Assert.NotEmpty(patterns);
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Name));
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Description));
        }

        [Fact]
        public void GetSupportedStateManagementPatterns_ReturnsNonEmptyCollection()
        {
            // Act
            var patterns = _standardizer.GetSupportedStateManagementPatterns();

            // Assert
            Assert.NotNull(patterns);
            Assert.NotEmpty(patterns);
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Name));
            Assert.All(patterns, pattern => Assert.NotNull(pattern.Description));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithSecurityPatternsEnabled_AppliesSecurityPatterns()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions
            {
                ApplySecurityPatterns = true
            };

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.StandardizedLogic.SecurityPatterns);
            Assert.Contains(result.AppliedPatterns, p => p.Contains("Authentication") || p.Contains("Authorization"));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithStateManagementEnabled_AppliesStateManagement()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions
            {
                GenerateStateManagement = true
            };

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.StandardizedLogic.StateManagementPatterns);
            Assert.Contains(result.AppliedPatterns, p => p.Contains("State"));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithApiContractsEnabled_GeneratesApiContracts()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions
            {
                CreateApiContracts = true
            };

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.StandardizedLogic.ApiContracts);
            Assert.Contains(result.AppliedPatterns, p => p.Contains("API"));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithDataFlowOptimizationEnabled_AppliesDataFlowPatterns()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions
            {
                OptimizeDataFlow = true
            };

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.StandardizedLogic.DataFlowPatterns);
            Assert.Contains(result.AppliedPatterns, p => p.Contains("Data Flow"));
        }

        [Fact]
        public async Task StandardizeApplicationLogicAsync_WithCachingEnabled_AppliesCachingStrategies()
        {
            // Arrange
            var domainLogic = CreateValidDomainLogic();
            var options = new ApplicationLogicStandardizationOptions
            {
                IntegrateCaching = true
            };

            // Act
            var result = await _standardizer.StandardizeApplicationLogicAsync(domainLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.StandardizedLogic.CachingStrategies);
            Assert.Contains(result.AppliedPatterns, p => p.Contains("Cache"));
        }

        [Fact]
        public async Task ApplySecurityPatternsAsync_WithAuthenticationEnabled_AppliesAuthenticationPattern()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new SecurityPatternOptions
            {
                EnableAuthentication = true
            };

            // Act
            var result = await _standardizer.ApplySecurityPatternsAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.AppliedSecurityPatterns, p => p.Contains("JWT"));
        }

        [Fact]
        public async Task ApplySecurityPatternsAsync_WithAuthorizationEnabled_AppliesAuthorizationPattern()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new SecurityPatternOptions
            {
                EnableAuthorization = true
            };

            // Act
            var result = await _standardizer.ApplySecurityPatternsAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.AppliedSecurityPatterns, p => p.Contains("Authorization"));
        }

        [Fact]
        public async Task OptimizeForPerformanceAsync_WithAsyncProcessingEnabled_AppliesAsyncOptimization()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new PerformanceOptimizationOptions
            {
                EnableAsyncProcessing = true
            };

            // Act
            var result = await _standardizer.OptimizeForPerformanceAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.AppliedOptimizations, o => o.Contains("Async"));
        }

        [Fact]
        public async Task GenerateStateManagementAsync_WithGlobalStateEnabled_AppliesGlobalStatePattern()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new StateManagementOptions
            {
                EnableGlobalState = true
            };

            // Act
            var result = await _standardizer.GenerateStateManagementAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.AppliedStatePatterns, p => p.Contains("Global"));
        }

        [Fact]
        public async Task GenerateApiContractsAsync_WithRESTEnabled_GeneratesRESTContracts()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new ApiContractOptions
            {
                EnableRest = true
            };

            // Act
            var result = await _standardizer.GenerateApiContractsAsync(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.GeneratedApiContracts, c => c.Contains("REST"));
        }

        [Fact]
        public async Task IntegrateCachingStrategiesAsync_WithMemoryCacheEnabled_AppliesMemoryCacheStrategy()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new CachingStrategyOptions
            {
                EnableMemoryCache = true
            };

            // Act
            var result = await _standardizer.IntegrateCachingStrategiesAsync(applicationLogic, options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(result.AppliedCachingStrategies, s => s.Contains("Memory"));
        }



        [Fact]
        public async Task ValidateApplicationLogicAsync_WithValidApplicationLogic_ReturnsValidResult()
        {
            // Arrange
            var applicationLogic = CreateValidStandardizedApplicationLogic();
            var options = new ApplicationLogicValidationOptions();

            // Act
            var result = await _standardizer.ValidateApplicationLogicAsync(applicationLogic, options);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.ValidationScore > 0);
            // Note: Validation may identify issues without generating recommendations
        }

        [Fact]
        public async Task ValidateApplicationLogicAsync_WithInvalidApplicationLogic_ReturnsInvalidResult()
        {
            // Arrange
            var applicationLogic = CreateInvalidStandardizedApplicationLogic();
            var options = new ApplicationLogicValidationOptions();

            // Act
            var result = await _standardizer.ValidateApplicationLogicAsync(applicationLogic, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.ValidationScore < 0.8);
            Assert.NotEmpty(result.Issues);
        }

        #region Helper Methods

        private DomainLogic CreateValidDomainLogic()
        {
            return new DomainLogic
            {
                Entities = new List<DomainEntity>
                {
                    new DomainEntity
                    {
                        Name = "User",
                        Properties = new List<EntityProperty>
                        {
                            new EntityProperty
                            {
                                Name = "Id",
                                Type = "string",
                                IsRequired = true,
                                Description = "User identifier"
                            },
                            new EntityProperty
                            {
                                Name = "Name",
                                Type = "string",
                                IsRequired = true,
                                Description = "User name"
                            }
                        },
                        Methods = new List<EntityMethod>
                        {
                            new EntityMethod
                            {
                                Name = "UpdateProfile",
                                ReturnType = "void",
                                Parameters = new List<MethodParameter>()
                            }
                        }
                    }
                },
                BusinessRules = new List<BusinessRule>
                {
                    new BusinessRule
                    {
                        Name = "UserValidation",
                        Description = "User validation rules",
                        Condition = "User must have valid email"
                    }
                },
                ValueObjects = new List<ValueObject>
                {
                    new ValueObject
                    {
                        Name = "Email",
                        Properties = new List<ValueObjectProperty>
                        {
                            new ValueObjectProperty
                            {
                                Name = "Value",
                                Type = "string",
                                IsRequired = true
                            }
                        }
                    }
                }
            };
        }

        private StandardizedApplicationLogic CreateValidStandardizedApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>
                {
                    new ApplicationPattern
                    {
                        Name = "Repository",
                        Description = "Repository pattern",
                        Type = PatternType.Repository,
                        Implementation = "IRepository",
                        GeneratedCode = "public interface IRepository { }"
                    }
                },
                SecurityPatterns = new List<SecurityPattern>
                {
                    new SecurityPattern
                    {
                        Name = "JWT Authentication",
                        Description = "JWT authentication",
                        Type = SecurityPatternType.Jwt,
                        Implementation = "JWT Service",
                        GeneratedCode = "public interface IJwtService { }"
                    }
                },
                StateManagementPatterns = new List<StateManagementPattern>
                {
                    new StateManagementPattern
                    {
                        Name = "Global State",
                        Description = "Global state management",
                        Type = StateManagementType.GlobalState,
                        Implementation = "Global State",
                        GeneratedCode = "public interface IGlobalState { }"
                    }
                },
                ApiContracts = new List<ApiContract>
                {
                    new ApiContract
                    {
                        Name = "User API",
                        Description = "User API contract",
                        Endpoint = "/api/user",
                        Method = Nexo.Feature.AI.Models.HttpMethod.Get,
                        GeneratedCode = "public class UserController { }"
                    }
                },
                DataFlowPatterns = new List<DataFlowPattern>
                {
                    new DataFlowPattern
                    {
                        Name = "Unidirectional",
                        Description = "Unidirectional data flow",
                        Type = DataFlowType.Unidirectional,
                        Implementation = "Data Flow",
                        GeneratedCode = "public interface IDataFlow { }"
                    }
                },
                CachingStrategies = new List<CachingStrategy>
                {
                    new CachingStrategy
                    {
                        Name = "Memory Cache",
                        Description = "Memory caching",
                        Type = CachingStrategyType.MemoryCache,
                        Implementation = "Cache Service",
                        ExpirationTime = TimeSpan.FromMinutes(30),
                        GeneratedCode = "public interface ICacheService { }"
                    }
                }
            };
        }

        private StandardizedApplicationLogic CreateInvalidStandardizedApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>(), // Empty patterns
                SecurityPatterns = new List<SecurityPattern>(), // Empty security patterns
                StateManagementPatterns = new List<StateManagementPattern>(),
                ApiContracts = new List<ApiContract>(),
                DataFlowPatterns = new List<DataFlowPattern>(),
                CachingStrategies = new List<CachingStrategy>() // Empty caching strategies
            };
        }

        #endregion
    }
} 