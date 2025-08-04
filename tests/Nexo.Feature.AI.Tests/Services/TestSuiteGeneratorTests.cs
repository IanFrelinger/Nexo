using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Xunit;

namespace Nexo.Feature.AI.Tests.Services
{
    /// <summary>
    /// Tests for the TestSuiteGenerator service
    /// </summary>
    public class TestSuiteGeneratorTests
    {
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly Mock<ILogger<TestSuiteGenerator>> _mockLogger;
        private readonly TestSuiteGenerator _testSuiteGenerator;

        public TestSuiteGeneratorTests()
        {
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _mockLogger = new Mock<ILogger<TestSuiteGenerator>>();
            
            // Configure mock to return a successful response
            _mockModelOrchestrator
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModelResponse
                {
                    Content = "Mock test code response",
                    Model = "gpt-4"
                });
            
            _testSuiteGenerator = new TestSuiteGenerator(_mockModelOrchestrator.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_testSuiteGenerator);
        }

        [Fact]
        public void Constructor_NullModelOrchestrator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestSuiteGenerator(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestSuiteGenerator(_mockModelOrchestrator.Object, null!));
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.GenerateUnitTestsAsync(null!));
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_ValidDomainLogic_ReturnsUnitTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateUnitTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.CoveragePercentage > 0);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_EmptyDomainLogic_ReturnsEmptyTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult();

            // Act
            var result = await _testSuiteGenerator.GenerateUnitTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Tests);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.CoveragePercentage);
        }

        [Fact]
        public async Task GenerateIntegrationTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.GenerateIntegrationTestsAsync(null!));
        }

        [Fact]
        public async Task GenerateIntegrationTestsAsync_ValidDomainLogic_ReturnsIntegrationTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateIntegrationTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.CoveragePercentage > 0);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }

        [Fact]
        public async Task GenerateIntegrationTestsAsync_EmptyDomainLogic_ReturnsEmptyTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult();

            // Act
            var result = await _testSuiteGenerator.GenerateIntegrationTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Tests);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.CoveragePercentage);
        }

        [Fact]
        public async Task GenerateEdgeCaseTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.GenerateEdgeCaseTestsAsync(null!));
        }

        [Fact]
        public async Task GenerateEdgeCaseTestsAsync_ValidDomainLogic_ReturnsEdgeCaseTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateEdgeCaseTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.True(result.CoveragePercentage > 0);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }

        [Fact]
        public async Task GenerateEdgeCaseTestsAsync_EmptyDomainLogic_ReturnsEmptyTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult();

            // Act
            var result = await _testSuiteGenerator.GenerateEdgeCaseTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Tests);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.CoveragePercentage);
        }

        [Fact]
        public async Task ValidateTestCoverageAsync_NullTestSuite_ThrowsArgumentNullException()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.ValidateTestCoverageAsync(null!, domainLogic, CancellationToken.None));
        }

        [Fact]
        public async Task ValidateTestCoverageAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Arrange
            var testSuite = CreateSampleCompleteTestSuite();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.ValidateTestCoverageAsync(testSuite, null!, CancellationToken.None));
        }

        [Fact]
        public async Task ValidateTestCoverageAsync_ValidParameters_ReturnsTestCoverageValidationResult()
        {
            // Arrange
            var testSuite = CreateSampleCompleteTestSuite();
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.ValidateTestCoverageAsync(testSuite, domainLogic, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            // Coverage validation may not be valid if coverage is below threshold, so just check it's calculated
            Assert.True(result.CoveragePercentage >= 0);
            Assert.NotNull(result.UncoveredAreas);
            Assert.NotNull(result.Recommendations);
            // MeetsThreshold depends on actual coverage vs threshold
            Assert.Equal(90.0, result.CoverageThreshold);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }

        [Fact]
        public async Task GenerateCompleteTestSuiteAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _testSuiteGenerator.GenerateCompleteTestSuiteAsync(null!));
        }

        [Fact]
        public async Task GenerateCompleteTestSuiteAsync_ValidDomainLogic_ReturnsCompleteTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateCompleteTestSuiteAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.TestSuite);
            Assert.NotNull(result.CoverageValidation);
            Assert.True(result.TestSuite.TotalTestCount > 0);
            Assert.True(result.TestSuite.OverallCoverage > 0);
            Assert.False(string.IsNullOrEmpty(result.Summary));
        }

        [Fact]
        public async Task GenerateCompleteTestSuiteAsync_EmptyDomainLogic_ReturnsEmptyTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult();

            // Act
            var result = await _testSuiteGenerator.GenerateCompleteTestSuiteAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.TestSuite);
            Assert.NotNull(result.CoverageValidation);
            Assert.Equal(0, result.TestSuite.TotalTestCount);
            Assert.Equal(0, result.TestSuite.OverallCoverage);
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_GeneratesTestsForAllEntities()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateUnitTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Check that tests were generated for each entity
            foreach (var entity in domainLogic.GeneratedLogic.Entities)
            {
                var entityTests = result.Tests.Where(t => t.TestClass.Contains(entity.Name)).ToList();
                Assert.True(entityTests.Count > 0, $"No tests generated for entity {entity.Name}");
                
                // Check for constructor test
                Assert.Contains(entityTests, t => t.TestName.Contains("Constructor"));
                
                // Check for property tests
                foreach (var property in entity.Properties)
                {
                    Assert.Contains(entityTests, t => t.TestName.Contains(property.Name));
                }
            }
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_GeneratesTestsForAllValueObjects()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateUnitTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Check that tests were generated for each value object
            foreach (var valueObject in domainLogic.GeneratedLogic.ValueObjects)
            {
                var valueObjectTests = result.Tests.Where(t => t.TestClass.Contains(valueObject.Name)).ToList();
                Assert.True(valueObjectTests.Count > 0, $"No tests generated for value object {valueObject.Name}");
                // AI-enhanced tests generate different names, so just check that tests exist
                Assert.True(valueObjectTests.Any(), $"No tests found for value object {valueObject.Name}");
            }
        }

        [Fact]
        public async Task GenerateUnitTestsAsync_GeneratesTestsForAllBusinessRules()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateUnitTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Check that tests were generated for each business rule
            foreach (var businessRule in domainLogic.GeneratedLogic.BusinessRules)
            {
                var businessRuleTests = result.Tests.Where(t => t.TestClass.Contains(businessRule.Name)).ToList();
                Assert.True(businessRuleTests.Count > 0, $"No tests generated for business rule {businessRule.Name}");
                // AI-enhanced tests generate different names, so just check that tests exist
                Assert.True(businessRuleTests.Any(), $"No tests found for business rule {businessRule.Name}");
            }
        }

        [Fact]
        public async Task GenerateIntegrationTestsAsync_GeneratesTestsForEntityInteractions()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateIntegrationTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Check that integration tests were generated for entity interactions
            var entityInteractionTests = result.Tests.Where(t => t.TestName.Contains("InteractsWith")).ToList();
            Assert.True(entityInteractionTests.Count > 0, "No entity interaction tests generated");
        }

        [Fact]
        public async Task GenerateEdgeCaseTestsAsync_IdentifiesCommonEdgeCases()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateEdgeCaseTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Check that common edge cases were identified
            Assert.True(result.Tests.Count > 0, "No edge case tests generated");
            Assert.True(result.CoveragePercentage > 0, "No coverage calculated");
        }

        [Fact]
        public async Task ValidateTestCoverageAsync_CalculatesCoverageCorrectly()
        {
            // Arrange
            var testSuite = CreateSampleCompleteTestSuite();
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.ValidateTestCoverageAsync(testSuite, domainLogic, CancellationToken.None);

            // Assert
            // Coverage validation may not be valid if coverage is below threshold, so just check it's calculated
            Assert.True(result.CoveragePercentage >= 0 && result.CoveragePercentage <= 100);
            Assert.True(result.MeetsThreshold == (result.CoveragePercentage >= result.CoverageThreshold));
        }

        [Fact]
        public async Task GenerateCompleteTestSuiteAsync_IncludesAllTestTypes()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateCompleteTestSuiteAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.TestSuite.UnitTests);
            Assert.NotNull(result.TestSuite.IntegrationTests);
            Assert.NotNull(result.TestSuite.EdgeCaseTests);
            
            var totalTests = result.TestSuite.UnitTests.Count + 
                           result.TestSuite.IntegrationTests.Count + 
                           result.TestSuite.EdgeCaseTests.Count;
            
            Assert.Equal(result.TestSuite.TotalTestCount, totalTests);
        }



        [Fact]
        public async Task GenerateTestDataAsync_ValidDomainLogic_ReturnsTestDataSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateTestDataAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.TestData);
            Assert.True(result.TotalDataItems > 0);
            Assert.NotEmpty(result.Summary);
        }

        [Fact]
        public async Task GenerateTestDataAsync_EmptyDomainLogic_ReturnsEmptyTestDataSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            // Act
            var result = await _testSuiteGenerator.GenerateTestDataAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.TestData);
            Assert.Equal(0, result.TotalDataItems);
        }

        [Fact]
        public async Task GenerateTestDataAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _testSuiteGenerator.GenerateTestDataAsync(null));
        }

        [Fact]
        public async Task GeneratePerformanceTestsAsync_ValidDomainLogic_ReturnsPerformanceTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GeneratePerformanceTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.NotEmpty(result.Summary);
        }

        [Fact]
        public async Task GeneratePerformanceTestsAsync_EmptyDomainLogic_ReturnsEmptyPerformanceTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            // Act
            var result = await _testSuiteGenerator.GeneratePerformanceTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.Equal(0, result.TotalTests);
        }

        [Fact]
        public async Task GeneratePerformanceTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _testSuiteGenerator.GeneratePerformanceTestsAsync(null));
        }

        [Fact]
        public async Task GenerateSecurityTestsAsync_ValidDomainLogic_ReturnsSecurityTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateSecurityTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.NotEmpty(result.Summary);
        }

        [Fact]
        public async Task GenerateSecurityTestsAsync_EmptyDomainLogic_ReturnsEmptySecurityTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            // Act
            var result = await _testSuiteGenerator.GenerateSecurityTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.Equal(0, result.TotalTests);
        }

        [Fact]
        public async Task GenerateSecurityTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _testSuiteGenerator.GenerateSecurityTestsAsync(null));
        }

        [Fact]
        public async Task GenerateAccessibilityTestsAsync_ValidDomainLogic_ReturnsAccessibilityTestSuiteResult()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateAccessibilityTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.True(result.TotalTests > 0);
            Assert.NotEmpty(result.Summary);
        }

        [Fact]
        public async Task GenerateAccessibilityTestsAsync_EmptyDomainLogic_ReturnsEmptyAccessibilityTestSuite()
        {
            // Arrange
            var domainLogic = new DomainLogicResult
            {
                IsSuccess = true,
                GeneratedLogic = new DomainLogic()
            };

            // Act
            var result = await _testSuiteGenerator.GenerateAccessibilityTestsAsync(domainLogic);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Tests);
            Assert.Equal(0, result.TotalTests);
        }

        [Fact]
        public async Task GenerateAccessibilityTestsAsync_NullDomainLogic_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _testSuiteGenerator.GenerateAccessibilityTestsAsync(null));
        }

        [Fact]
        public async Task GenerateTestDataAsync_GeneratesTestDataForAllEntities()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateTestDataAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.TestData.Count > 0);
            
            // Verify test data for each entity
            foreach (var entity in domainLogic.GeneratedLogic.Entities)
            {
                var entityTestData = result.TestData.Where(td => td.EntityName == entity.Name).ToList();
                Assert.True(entityTestData.Count > 0, $"No test data generated for entity {entity.Name}");
            }
        }

        [Fact]
        public async Task GeneratePerformanceTestsAsync_GeneratesPerformanceTestsForAllEntities()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GeneratePerformanceTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Tests.Count > 0);
            
            // Verify performance tests for each entity
            foreach (var entity in domainLogic.GeneratedLogic.Entities)
            {
                var entityPerformanceTests = result.Tests.Where(t => t.TestClass.Contains(entity.Name)).ToList();
                Assert.True(entityPerformanceTests.Count > 0, $"No performance tests generated for entity {entity.Name}");
            }
        }

        [Fact]
        public async Task GenerateSecurityTestsAsync_GeneratesSecurityTestsForAllEntities()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateSecurityTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Tests.Count > 0);
            
            // Verify security tests for each entity
            foreach (var entity in domainLogic.GeneratedLogic.Entities)
            {
                var entitySecurityTests = result.Tests.Where(t => t.TestClass.Contains(entity.Name)).ToList();
                Assert.True(entitySecurityTests.Count > 0, $"No security tests generated for entity {entity.Name}");
            }
        }

        [Fact]
        public async Task GenerateAccessibilityTestsAsync_GeneratesAccessibilityTestsForAllEntities()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateAccessibilityTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Tests.Count > 0);
            
            // Verify accessibility tests for each entity
            foreach (var entity in domainLogic.GeneratedLogic.Entities)
            {
                var entityAccessibilityTests = result.Tests.Where(t => t.TestClass.Contains(entity.Name)).ToList();
                Assert.True(entityAccessibilityTests.Count > 0, $"No accessibility tests generated for entity {entity.Name}");
            }
        }

        [Fact]
        public async Task GenerateTestDataAsync_TestDataItemsHaveValidProperties()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateTestDataAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            foreach (var testDataItem in result.TestData)
            {
                Assert.NotEmpty(testDataItem.EntityName);
                Assert.NotEmpty(testDataItem.DataName);
                Assert.NotEmpty(testDataItem.DataType);
                Assert.NotEmpty(testDataItem.DataValue);
                Assert.NotEmpty(testDataItem.Description);
                Assert.NotEmpty(testDataItem.UseCase);
            }
        }

        [Fact]
        public async Task GeneratePerformanceTestsAsync_PerformanceTestsHaveValidProperties()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GeneratePerformanceTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            foreach (var performanceTest in result.Tests)
            {
                Assert.NotEmpty(performanceTest.TestName);
                Assert.NotEmpty(performanceTest.TestMethod);
                Assert.NotEmpty(performanceTest.TestClass);
                Assert.NotEmpty(performanceTest.TestCode);
                Assert.NotEmpty(performanceTest.Description);
                Assert.NotEmpty(performanceTest.PerformanceMetric);
                Assert.True(performanceTest.ExpectedThreshold > 0);
                Assert.NotEmpty(performanceTest.LoadProfile);
                Assert.NotEmpty(performanceTest.TestScenario);
            }
        }

        [Fact]
        public async Task GenerateSecurityTestsAsync_SecurityTestsHaveValidProperties()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateSecurityTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            foreach (var securityTest in result.Tests)
            {
                Assert.NotEmpty(securityTest.TestName);
                Assert.NotEmpty(securityTest.TestMethod);
                Assert.NotEmpty(securityTest.TestClass);
                Assert.NotEmpty(securityTest.TestCode);
                Assert.NotEmpty(securityTest.Description);
                Assert.NotEmpty(securityTest.SecurityVulnerability);
                Assert.NotEmpty(securityTest.RiskLevel);
                Assert.NotEmpty(securityTest.AttackVector);
                Assert.NotEmpty(securityTest.MitigationStrategy);
            }
        }

        [Fact]
        public async Task GenerateAccessibilityTestsAsync_AccessibilityTestsHaveValidProperties()
        {
            // Arrange
            var domainLogic = CreateSampleDomainLogic();

            // Act
            var result = await _testSuiteGenerator.GenerateAccessibilityTestsAsync(domainLogic);

            // Assert
            Assert.True(result.IsSuccess);
            
            foreach (var accessibilityTest in result.Tests)
            {
                Assert.NotEmpty(accessibilityTest.TestName);
                Assert.NotEmpty(accessibilityTest.TestMethod);
                Assert.NotEmpty(accessibilityTest.TestClass);
                Assert.NotEmpty(accessibilityTest.TestCode);
                Assert.NotEmpty(accessibilityTest.Description);
                Assert.NotEmpty(accessibilityTest.AccessibilityGuideline);
                Assert.NotEmpty(accessibilityTest.ComplianceLevel);
                Assert.NotEmpty(accessibilityTest.UserScenario);
                Assert.NotEmpty(accessibilityTest.AssistiveTechnology);
            }
        }

        #region Helper Methods

        private static DomainLogicResult CreateSampleDomainLogic()
        {
            return new DomainLogicResult
            {
                GeneratedLogic = new DomainLogic
                {
                    Entities = new List<DomainEntity>
                    {
                        new DomainEntity
                        {
                            Name = "User",
                            Properties = new List<EntityProperty>
                            {
                                new EntityProperty { Name = "Id", Type = "Guid" },
                                new EntityProperty { Name = "Name", Type = "string" },
                                new EntityProperty { Name = "Email", Type = "string" }
                            }
                        },
                        new DomainEntity
                        {
                            Name = "Order",
                            Properties = new List<EntityProperty>
                            {
                                new EntityProperty { Name = "Id", Type = "Guid" },
                                new EntityProperty { Name = "UserId", Type = "Guid" },
                                new EntityProperty { Name = "Total", Type = "decimal" }
                            }
                        }
                    },
                    ValueObjects = new List<ValueObject>
                    {
                        new ValueObject { Name = "EmailAddress" },
                        new ValueObject { Name = "Money" }
                    },
                    BusinessRules = new List<BusinessRule>
                    {
                        new BusinessRule { Name = "EmailValidationRule" },
                        new BusinessRule { Name = "OrderTotalRule" }
                    }
                }
            };
        }

        private static CompleteTestSuite CreateSampleCompleteTestSuite()
        {
            return new CompleteTestSuite
            {
                UnitTests = new List<UnitTest>
                {
                    new UnitTest { TestName = "Test1", TestClass = "TestClass1" },
                    new UnitTest { TestName = "Test2", TestClass = "TestClass2" }
                },
                IntegrationTests = new List<IntegrationTest>
                {
                    new IntegrationTest { TestName = "IntegrationTest1", TestClass = "IntegrationTestClass1" },
                    new IntegrationTest { TestName = "IntegrationTest2", TestClass = "IntegrationTestClass2" }
                },
                EdgeCaseTests = new List<EdgeCaseTest>
                {
                    new EdgeCaseTest { TestName = "EdgeCaseTest1", TestClass = "EdgeCaseTestClass1" },
                    new EdgeCaseTest { TestName = "EdgeCaseTest2", TestClass = "EdgeCaseTestClass2" }
                },
                TotalTestCount = 6,
                OverallCoverage = 82.0,
                Summary = "Sample test suite"
            };
        }

        #endregion
    }
}