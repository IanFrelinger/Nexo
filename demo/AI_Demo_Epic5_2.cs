using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.FeatureFactory.DomainLogic;
using Nexo.Core.Application.Services.FeatureFactory.Orchestration;
using Nexo.Core.Application.Services.FeatureFactory.TestGeneration;
using Nexo.Core.Application.Services.FeatureFactory.Validation;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Demo
{
    /// <summary>
    /// Epic 5.2: Domain Logic Generation Demo
    /// 
    /// This demo showcases the complete domain logic generation workflow:
    /// 1. Generate domain logic from validated requirements
    /// 2. Generate comprehensive test suites
    /// 3. Validate generated domain logic
    /// 4. Optimize generated code
    /// 5. Generate validation reports
    /// </summary>
    public class AI_Demo_Epic5_2
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Nexo Epic 5.2: Domain Logic Generation Demo");
            Console.WriteLine("================================================");
            Console.WriteLine();

            // Create host and services
            var host = CreateHost();
            var services = host.Services;

            try
            {
                // Get required services
                var orchestrator = services.GetRequiredService<IDomainLogicOrchestrator>();
                var domainLogicGenerator = services.GetRequiredService<IDomainLogicGenerator>();
                var testSuiteGenerator = services.GetRequiredService<ITestSuiteGenerator>();
                var validator = services.GetRequiredService<IDomainLogicValidator>();
                var logger = services.GetRequiredService<ILogger<AI_Demo_Epic5_2>>();

                logger.LogInformation("Starting Epic 5.2 Domain Logic Generation Demo");

                // Demo 1: Complete Domain Logic Generation Workflow
                await DemoCompleteWorkflow(orchestrator, logger);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue to individual component demos...");
                Console.ReadKey();

                // Demo 2: Individual Component Demos
                await DemoIndividualComponents(domainLogicGenerator, testSuiteGenerator, validator, logger);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue to validation and optimization demos...");
                Console.ReadKey();

                // Demo 3: Validation and Optimization
                await DemoValidationAndOptimization(validator, logger);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue to session management demos...");
                Console.ReadKey();

                // Demo 4: Session Management
                await DemoSessionManagement(orchestrator, logger);

                Console.WriteLine();
                Console.WriteLine("🎉 Epic 5.2 Demo completed successfully!");
                Console.WriteLine("================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add AI services
                    services.AddAIServices();

                    // Add Feature Factory services
                    services.AddScoped<IDomainLogicGenerator, DomainLogicGenerator>();
                    services.AddScoped<ITestSuiteGenerator, TestSuiteGenerator>();
                    services.AddScoped<IDomainLogicValidator, DomainLogicValidator>();
                    services.AddScoped<IDomainLogicOrchestrator, DomainLogicOrchestrator>();
                })
                .Build();
        }

        private static async Task DemoCompleteWorkflow(IDomainLogicOrchestrator orchestrator, ILogger logger)
        {
            Console.WriteLine("📋 Demo 1: Complete Domain Logic Generation Workflow");
            Console.WriteLine("----------------------------------------------------");

            // Create sample validated requirements
            var requirements = CreateSampleValidatedRequirements();

            Console.WriteLine($"📝 Processing requirements: {requirements.Requirements.Count} requirements");
            Console.WriteLine();

            // Start generation process
            var result = await orchestrator.GenerateCompleteDomainLogicAsync(requirements);

            if (result.Success)
            {
                Console.WriteLine("✅ Domain logic generation completed successfully!");
                Console.WriteLine($"📊 Session ID: {result.SessionId}");
                Console.WriteLine($"📈 Generated at: {result.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();

                // Display metrics
                DisplayGenerationMetrics(result.Metrics);

                // Display domain logic summary
                DisplayDomainLogicSummary(result.DomainLogic);

                // Display test suite summary
                DisplayTestSuiteSummary(result.TestSuite);

                // Display validation summary
                DisplayValidationSummary(result.ValidationReport);

                // Display optimization summary
                DisplayOptimizationSummary(result.OptimizationResult);
            }
            else
            {
                Console.WriteLine($"❌ Domain logic generation failed: {result.ErrorMessage}");
            }
        }

        private static async Task DemoIndividualComponents(
            IDomainLogicGenerator domainLogicGenerator,
            ITestSuiteGenerator testSuiteGenerator,
            IDomainLogicValidator validator,
            ILogger logger)
        {
            Console.WriteLine("🔧 Demo 2: Individual Component Demos");
            Console.WriteLine("-------------------------------------");

            // Create sample requirements
            var requirements = CreateSampleValidatedRequirements();
            var requirement = requirements.Requirements[0];

            // Demo Domain Logic Generator
            Console.WriteLine("🏗️  Domain Logic Generator Demo");
            Console.WriteLine("Generating business entities...");
            var entityResult = await domainLogicGenerator.GenerateBusinessEntitiesAsync(requirement);
            if (entityResult.Success)
            {
                Console.WriteLine($"✅ Generated {entityResult.Entities.Count} business entities");
                foreach (var entity in entityResult.Entities)
                {
                    Console.WriteLine($"   - {entity.Name}: {entity.Description}");
                }
            }

            Console.WriteLine("Generating value objects...");
            var valueObjectResult = await domainLogicGenerator.GenerateValueObjectsAsync(requirement);
            if (valueObjectResult.Success)
            {
                Console.WriteLine($"✅ Generated {valueObjectResult.ValueObjects.Count} value objects");
                foreach (var valueObject in valueObjectResult.ValueObjects)
                {
                    Console.WriteLine($"   - {valueObject.Name}: {valueObject.Description}");
                }
            }

            Console.WriteLine("Generating business rules...");
            var ruleResult = await domainLogicGenerator.GenerateBusinessRulesAsync(requirement);
            if (ruleResult.Success)
            {
                Console.WriteLine($"✅ Generated {ruleResult.BusinessRules.Count} business rules");
                foreach (var rule in ruleResult.BusinessRules)
                {
                    Console.WriteLine($"   - {rule.Name}: {rule.Description}");
                }
            }

            Console.WriteLine();

            // Demo Test Suite Generator
            Console.WriteLine("🧪 Test Suite Generator Demo");
            var domainLogic = await domainLogicGenerator.GenerateDomainLogicAsync(requirements);
            if (domainLogic.Success)
            {
                Console.WriteLine("Generating test suite...");
                var testSuiteResult = await testSuiteGenerator.GenerateTestSuiteAsync(domainLogic);
                if (testSuiteResult.Success)
                {
                    Console.WriteLine($"✅ Generated {testSuiteResult.UnitTests.Count} unit tests");
                    Console.WriteLine($"✅ Generated {testSuiteResult.IntegrationTests.Count} integration tests");
                    Console.WriteLine($"✅ Generated {testSuiteResult.DomainTests.Count} domain tests");
                    Console.WriteLine($"✅ Generated {testSuiteResult.TestFixtures.Count} test fixtures");
                    Console.WriteLine($"📊 Test coverage: {testSuiteResult.Coverage.LineCoverage:F1}%");
                }
            }

            Console.WriteLine();

            // Demo Domain Logic Validator
            Console.WriteLine("🔍 Domain Logic Validator Demo");
            if (domainLogic.Success)
            {
                Console.WriteLine("Validating domain logic...");
                var validationResult = await validator.ValidateDomainLogicAsync(domainLogic);
                if (validationResult.Success)
                {
                    Console.WriteLine($"✅ Validation completed");
                    Console.WriteLine($"📊 Overall score: {validationResult.Score.Overall:F1}%");
                    Console.WriteLine($"   - Correctness: {validationResult.Score.Correctness:F1}%");
                    Console.WriteLine($"   - Completeness: {validationResult.Score.Completeness:F1}%");
                    Console.WriteLine($"   - Consistency: {validationResult.Score.Consistency:F1}%");
                    Console.WriteLine($"   - Maintainability: {validationResult.Score.Maintainability:F1}%");
                    Console.WriteLine($"⚠️  Issues: {validationResult.Issues.Count}");
                    Console.WriteLine($"⚠️  Warnings: {validationResult.Warnings.Count}");
                    Console.WriteLine($"💡 Suggestions: {validationResult.Suggestions.Count}");
                }
            }
        }

        private static async Task DemoValidationAndOptimization(IDomainLogicValidator validator, ILogger logger)
        {
            Console.WriteLine("🔍 Demo 3: Validation and Optimization");
            Console.WriteLine("--------------------------------------");

            // Create sample domain logic
            var domainLogic = CreateSampleDomainLogic();

            // Demo Business Rule Validation
            Console.WriteLine("📋 Business Rule Validation Demo");
            var ruleValidationResult = await validator.ValidateBusinessRulesAsync(domainLogic.BusinessRules);
            if (ruleValidationResult.Success)
            {
                Console.WriteLine($"✅ Business rule validation completed");
                Console.WriteLine($"📊 Score: {ruleValidationResult.Score.Overall:F1}%");
                Console.WriteLine($"⚠️  Issues: {ruleValidationResult.Issues.Count}");
                Console.WriteLine($"⚠️  Warnings: {ruleValidationResult.Warnings.Count}");
            }

            // Demo Consistency Check
            Console.WriteLine("🔗 Consistency Check Demo");
            var consistencyResult = await validator.CheckConsistencyAsync(domainLogic);
            if (consistencyResult.Success)
            {
                Console.WriteLine($"✅ Consistency check completed");
                Console.WriteLine($"📊 Score: {consistencyResult.Score.Overall:F1}%");
                Console.WriteLine($"⚠️  Issues: {consistencyResult.Issues.Count}");
                Console.WriteLine($"⚠️  Warnings: {consistencyResult.Warnings.Count}");
            }

            // Demo Optimization
            Console.WriteLine("⚡ Optimization Demo");
            var optimizationResult = await validator.OptimizeDomainLogicAsync(domainLogic);
            if (optimizationResult.Success)
            {
                Console.WriteLine($"✅ Optimization completed");
                Console.WriteLine($"📊 Score: {optimizationResult.Score.Overall:F1}%");
                Console.WriteLine($"💡 Suggestions: {optimizationResult.Suggestions.Count}");
                Console.WriteLine($"🚀 Improvements: {optimizationResult.Improvements.Count}");
            }

            // Demo Validation Report Generation
            Console.WriteLine("📊 Validation Report Generation Demo");
            var validationReport = await validator.GenerateValidationReportAsync(domainLogic);
            Console.WriteLine($"✅ Validation report generated");
            Console.WriteLine($"📊 Overall Score: {validationReport.OverallScore.Overall:F1}%");
            Console.WriteLine($"   - Domain Logic: {validationReport.OverallScore.DomainLogic:F1}%");
            Console.WriteLine($"   - Business Rules: {validationReport.OverallScore.BusinessRules:F1}%");
            Console.WriteLine($"   - Consistency: {validationReport.OverallScore.Consistency:F1}%");
            Console.WriteLine($"   - Optimization: {validationReport.OverallScore.Optimization:F1}%");
            Console.WriteLine($"💡 Recommendations: {validationReport.Recommendations.Count}");
        }

        private static async Task DemoSessionManagement(IDomainLogicOrchestrator orchestrator, ILogger logger)
        {
            Console.WriteLine("📊 Demo 4: Session Management");
            Console.WriteLine("-----------------------------");

            // Get active sessions
            Console.WriteLine("📋 Active Sessions");
            var sessions = await orchestrator.GetActiveSessionsAsync();
            Console.WriteLine($"Found {sessions.Count} sessions");
            foreach (var session in sessions.Take(5))
            {
                Console.WriteLine($"   - {session.SessionId}: {session.Status} (Started: {session.StartedAt:HH:mm:ss})");
            }

            // Demo progress tracking
            Console.WriteLine();
            Console.WriteLine("📈 Progress Tracking Demo");
            if (sessions.Any())
            {
                var firstSession = sessions.First();
                var progress = await orchestrator.GetGenerationProgressAsync(firstSession.SessionId);
                Console.WriteLine($"Session: {progress.SessionId}");
                Console.WriteLine($"Status: {progress.Status}");
                Console.WriteLine($"Progress: {progress.ProgressPercentage:F1}%");
                Console.WriteLine($"Current Step: {progress.CurrentStep}");
                Console.WriteLine($"Steps:");
                foreach (var step in progress.Steps)
                {
                    Console.WriteLine($"   - {step.Name}: {step.Status} ({step.ProgressPercentage:F1}%)");
                }
            }

            // Demo session cleanup
            Console.WriteLine();
            Console.WriteLine("🧹 Session Cleanup Demo");
            var cleanupResult = await orchestrator.CleanupSessionsAsync();
            if (cleanupResult)
            {
                Console.WriteLine("✅ Session cleanup completed");
            }
            else
            {
                Console.WriteLine("❌ Session cleanup failed");
            }
        }

        private static ValidatedRequirements CreateSampleValidatedRequirements()
        {
            return new ValidatedRequirements
            {
                Success = true,
                Requirements = new List<ExtractedRequirement>
                {
                    new ExtractedRequirement
                    {
                        Name = "UserManagement",
                        Description = "User management system with authentication and authorization",
                        Type = "Functional",
                        Priority = "High",
                        Category = "Core",
                        BusinessRules = new List<string>
                        {
                            "Users must have unique email addresses",
                            "Passwords must meet security requirements",
                            "User roles must be assigned during registration"
                        },
                        UserStories = new List<string>
                        {
                            "As a user, I want to register an account",
                            "As a user, I want to login to my account",
                            "As a user, I want to update my profile"
                        },
                        AcceptanceCriteria = new List<string>
                        {
                            "User can register with valid email and password",
                            "User can login with correct credentials",
                            "User can update profile information"
                        }
                    },
                    new ExtractedRequirement
                    {
                        Name = "ProductCatalog",
                        Description = "Product catalog with search and filtering capabilities",
                        Type = "Functional",
                        Priority = "Medium",
                        Category = "Feature",
                        BusinessRules = new List<string>
                        {
                            "Products must have unique SKUs",
                            "Product prices must be positive",
                            "Products must have at least one category"
                        },
                        UserStories = new List<string>
                        {
                            "As a customer, I want to search for products",
                            "As a customer, I want to filter products by category",
                            "As a customer, I want to view product details"
                        },
                        AcceptanceCriteria = new List<string>
                        {
                            "Search returns relevant products",
                            "Filters work correctly",
                            "Product details are displayed properly"
                        }
                    }
                },
                ValidatedAt = DateTime.UtcNow
            };
        }

        private static DomainLogicResult CreateSampleDomainLogic()
        {
            return new DomainLogicResult
            {
                Success = true,
                Entities = new List<DomainEntity>
                {
                    new DomainEntity
                    {
                        Name = "User",
                        Description = "User entity for authentication and authorization",
                        Namespace = "Domain.Entities",
                        Type = EntityType.AggregateRoot,
                        Properties = new List<EntityProperty>
                        {
                            new EntityProperty { Name = "Id", Type = "Guid", Description = "Unique identifier", IsRequired = true },
                            new EntityProperty { Name = "Email", Type = "string", Description = "User email address", IsRequired = true },
                            new EntityProperty { Name = "PasswordHash", Type = "string", Description = "Hashed password", IsRequired = true }
                        }
                    }
                },
                ValueObjects = new List<ValueObject>
                {
                    new ValueObject
                    {
                        Name = "Email",
                        Description = "Email value object with validation",
                        Namespace = "Domain.ValueObjects",
                        Properties = new List<ValueObjectProperty>
                        {
                            new ValueObjectProperty { Name = "Value", Type = "string", Description = "Email address value", IsRequired = true }
                        }
                    }
                },
                BusinessRules = new List<BusinessRule>
                {
                    new BusinessRule
                    {
                        Name = "EmailMustBeUnique",
                        Description = "Email addresses must be unique",
                        Expression = "IsEmailUnique(email)",
                        Type = BusinessRuleType.Validation,
                        Priority = BusinessRulePriority.High
                    }
                },
                DomainServices = new List<DomainService>
                {
                    new DomainService
                    {
                        Name = "UserService",
                        Description = "Service for user-related operations",
                        Namespace = "Domain.Services",
                        Methods = new List<ServiceMethod>
                        {
                            new ServiceMethod
                            {
                                Name = "CreateUser",
                                ReturnType = "Task<User>",
                                Description = "Creates a new user",
                                IsAsync = true
                            }
                        }
                    }
                },
                GeneratedAt = DateTime.UtcNow
            };
        }

        private static void DisplayGenerationMetrics(GenerationMetrics metrics)
        {
            Console.WriteLine("📊 Generation Metrics:");
            Console.WriteLine($"   - Entities: {metrics.EntityCount}");
            Console.WriteLine($"   - Value Objects: {metrics.ValueObjectCount}");
            Console.WriteLine($"   - Business Rules: {metrics.BusinessRuleCount}");
            Console.WriteLine($"   - Domain Services: {metrics.DomainServiceCount}");
            Console.WriteLine($"   - Aggregate Roots: {metrics.AggregateRootCount}");
            Console.WriteLine($"   - Domain Events: {metrics.DomainEventCount}");
            Console.WriteLine($"   - Repositories: {metrics.RepositoryCount}");
            Console.WriteLine($"   - Factories: {metrics.FactoryCount}");
            Console.WriteLine($"   - Specifications: {metrics.SpecificationCount}");
            Console.WriteLine($"   - Unit Tests: {metrics.UnitTestCount}");
            Console.WriteLine($"   - Integration Tests: {metrics.IntegrationTestCount}");
            Console.WriteLine($"   - Domain Tests: {metrics.DomainTestCount}");
            Console.WriteLine($"   - Test Fixtures: {metrics.TestFixtureCount}");
            Console.WriteLine($"   - Test Coverage: {metrics.TestCoveragePercentage:F1}%");
            Console.WriteLine($"   - Validation Score: {metrics.ValidationScore:F1}%");
            Console.WriteLine($"   - Optimization Score: {metrics.OptimizationScore:F1}%");
            Console.WriteLine($"   - Total Time: {metrics.TotalGenerationTime.TotalSeconds:F1}s");
        }

        private static void DisplayDomainLogicSummary(DomainLogicResult domainLogic)
        {
            Console.WriteLine("🏗️  Domain Logic Summary:");
            Console.WriteLine($"   - Entities: {domainLogic.Entities.Count}");
            Console.WriteLine($"   - Value Objects: {domainLogic.ValueObjects.Count}");
            Console.WriteLine($"   - Business Rules: {domainLogic.BusinessRules.Count}");
            Console.WriteLine($"   - Domain Services: {domainLogic.DomainServices.Count}");
            Console.WriteLine($"   - Aggregate Roots: {domainLogic.AggregateRoots.Count}");
            Console.WriteLine($"   - Domain Events: {domainLogic.DomainEvents.Count}");
            Console.WriteLine($"   - Repositories: {domainLogic.Repositories.Count}");
            Console.WriteLine($"   - Factories: {domainLogic.Factories.Count}");
            Console.WriteLine($"   - Specifications: {domainLogic.Specifications.Count}");
        }

        private static void DisplayTestSuiteSummary(TestSuiteResult testSuite)
        {
            Console.WriteLine("🧪 Test Suite Summary:");
            Console.WriteLine($"   - Unit Tests: {testSuite.UnitTests.Count}");
            Console.WriteLine($"   - Integration Tests: {testSuite.IntegrationTests.Count}");
            Console.WriteLine($"   - Domain Tests: {testSuite.DomainTests.Count}");
            Console.WriteLine($"   - Test Fixtures: {testSuite.TestFixtures.Count}");
            Console.WriteLine($"   - Line Coverage: {testSuite.Coverage.LineCoverage:F1}%");
            Console.WriteLine($"   - Branch Coverage: {testSuite.Coverage.BranchCoverage:F1}%");
            Console.WriteLine($"   - Method Coverage: {testSuite.Coverage.MethodCoverage:F1}%");
            Console.WriteLine($"   - Class Coverage: {testSuite.Coverage.ClassCoverage:F1}%");
        }

        private static void DisplayValidationSummary(ValidationReport validationReport)
        {
            Console.WriteLine("🔍 Validation Summary:");
            Console.WriteLine($"   - Overall Score: {validationReport.OverallScore.Overall:F1}%");
            Console.WriteLine($"   - Domain Logic: {validationReport.OverallScore.DomainLogic:F1}%");
            Console.WriteLine($"   - Business Rules: {validationReport.OverallScore.BusinessRules:F1}%");
            Console.WriteLine($"   - Consistency: {validationReport.OverallScore.Consistency:F1}%");
            Console.WriteLine($"   - Optimization: {validationReport.OverallScore.Optimization:F1}%");
            Console.WriteLine($"   - Issues: {validationReport.DomainValidation.Issues.Count}");
            Console.WriteLine($"   - Warnings: {validationReport.DomainValidation.Warnings.Count}");
            Console.WriteLine($"   - Suggestions: {validationReport.DomainValidation.Suggestions.Count}");
            Console.WriteLine($"   - Recommendations: {validationReport.Recommendations.Count}");
        }

        private static void DisplayOptimizationSummary(OptimizationResult optimizationResult)
        {
            Console.WriteLine("⚡ Optimization Summary:");
            Console.WriteLine($"   - Score: {optimizationResult.Score.Overall:F1}%");
            Console.WriteLine($"   - Suggestions: {optimizationResult.Suggestions.Count}");
            Console.WriteLine($"   - Improvements: {optimizationResult.Improvements.Count}");
        }
    }

    // Sample data classes for the demo
    public class ValidatedRequirements
    {
        public bool Success { get; set; }
        public List<ExtractedRequirement> Requirements { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
    }

    public class ExtractedRequirement
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> BusinessRules { get; set; } = new();
        public List<string> UserStories { get; set; } = new();
        public List<string> AcceptanceCriteria { get; set; } = new();
    }
}
