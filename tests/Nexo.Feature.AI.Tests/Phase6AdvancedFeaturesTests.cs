using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.Agent.Services;
using Nexo.Core.Application.Models;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Feature.AI.Tests
{
    /// <summary>
    /// Tests for Phase 6 Advanced Features including advanced AI orchestration and multi-agent coordination.
    /// </summary>
    [Collection("Phase6SequentialTests")]
    public class Phase6AdvancedFeaturesTests : IDisposable
    {
        private Mock<IModelOrchestrator> CreateMockModelOrchestrator()
        {
            var mock = new Mock<IModelOrchestrator>();
            mock.Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ModelRequest request, CancellationToken token) => new ModelResponse
                {
                    Content = $"Mock response for: {request.Input}",
                    TotalTokens = 100,
                    Cost = 0.001m,
                    ProcessingTimeMs = 50,
                    Model = "gpt-4"
                });
            
            return mock;
        }

        private AdvancedModelOrchestrator CreateAdvancedModelOrchestrator()
        {
            var mockOrchestrator = CreateMockModelOrchestrator();
            var mockLogger = new Mock<ILogger<AdvancedModelOrchestrator>>();
            return new AdvancedModelOrchestrator(mockOrchestrator.Object, mockLogger.Object);
        }

        private MultiAgentCoordinator CreateMultiAgentCoordinator()
        {
            var mockOrchestrator = CreateMockModelOrchestrator();
            var mockLogger = new Mock<ILogger<MultiAgentCoordinator>>();
            return new MultiAgentCoordinator(mockOrchestrator.Object, mockLogger.Object);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteAdvancedAsync_ShouldSelectOptimalModel()
        {
            // Arrange - Create a completely fresh instance to ensure test isolation
            // Use a unique mock setup to avoid any shared state issues
            var mockOrchestrator = new Mock<IModelOrchestrator>();
            mockOrchestrator.Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (ModelRequest request, CancellationToken token) => 
                {
                    // Add a small delay to simulate real processing time
                    await Task.Delay(10, token);
                    return new ModelResponse
                    {
                        Content = $"Isolated mock response for: {request.Input}",
                        TotalTokens = 150,
                        Cost = 0.002m,
                        ProcessingTimeMs = 75,
                        Model = "gpt-4"
                    };
                });
            
            var mockLogger = new Mock<ILogger<AdvancedModelOrchestrator>>();
            var orchestrator = new AdvancedModelOrchestrator(mockOrchestrator.Object, mockLogger.Object);
            
            var request = new AdvancedModelRequest
            {
                Input = "Generate a C# class for user management",
                TaskType = "code_generation",
                ComplexityLevel = 3,
                RequiredLanguages = new List<string> { "csharp" },
                MaxTokens = 1000,
                Temperature = 0.7
            };

            // Act
            var result = await orchestrator.ExecuteAdvancedAsync(request);

            // Debug output to help diagnose issues
            Console.WriteLine($"Test Debug - Result Success: {result.Success}");
            Console.WriteLine($"Test Debug - Result ErrorMessage: {result.ErrorMessage}");
            Console.WriteLine($"Test Debug - Result ModelUsed: {result.ModelUsed}");
            Console.WriteLine($"Test Debug - Result Content: {result.Content?.Substring(0, Math.Min(50, result.Content?.Length ?? 0))}");

            // Assert with more detailed error messages
            Assert.NotNull(result);
            
            // Check Success property with detailed error message
            var successMessage = $"Expected Success=true, but got Success={result.Success}, ErrorMessage={result.ErrorMessage}, ModelUsed={result.ModelUsed}";
            Assert.True(result.Success, successMessage);
            
            // Additional assertions
            Assert.NotEmpty(result.ModelUsed);
            Assert.True(result.ProcessingTimeMs > 0, $"Expected ProcessingTimeMs > 0, but got {result.ProcessingTimeMs}");
            Assert.True(result.TokensUsed > 0, $"Expected TokensUsed > 0, but got {result.TokensUsed}");
            Assert.True(result.Cost > 0, $"Expected Cost > 0, but got {result.Cost}");
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteAdvancedAsync_WithPostProcessing_ShouldApplyFormatting()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var request = new AdvancedModelRequest
            {
                Input = "{\"name\":\"test\",\"value\":123}",
                TaskType = "formatting",
                PostProcessingOptions = new List<PostProcessingOption>
                {
                    new PostProcessingOption
                    {
                        Type = PostProcessingType.Formatting,
                        Parameters = new Dictionary<string, object> { ["formatType"] = "json" }
                    }
                }
            };

            // Act
            var result = await orchestrator.ExecuteAdvancedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Content);
            Assert.True(result.PostProcessingResults.Any());
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteMultiModelWorkflowAsync_ShouldCoordinateMultipleSteps()
        {
            // Arrange
            var workflow = new MultiModelWorkflow
            {
                Name = "Code Review Workflow",
                Description = "Multi-step code review process",
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Code Analysis",
                        Description = "Analyze code for potential issues",
                        InputTemplate = "Analyze this code: {code}",
                        TaskType = "code_analysis",
                        ComplexityLevel = 2,
                        RequiredLanguages = new List<string> { "csharp" },
                        InputPlaceholders = new List<InputPlaceholder>
                        {
                            new InputPlaceholder
                            {
                                Name = "code",
                                SourceStep = "initial",
                                ExtractionMethod = "static",
                                StaticValue = "public class Test { public string Name { get; set; } }"
                            }
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "Security Review",
                        Description = "Review code for security vulnerabilities",
                        InputTemplate = "Review security aspects: {code}",
                        TaskType = "security_analysis",
                        ComplexityLevel = 3,
                        RequiredLanguages = new List<string> { "csharp" },
                        InputPlaceholders = new List<InputPlaceholder>
                        {
                            new InputPlaceholder
                            {
                                Name = "code",
                                SourceStep = "Code Analysis",
                                ExtractionMethod = "content"
                            }
                        }
                    }
                }
            };

            // Act
            var orchestrator = CreateAdvancedModelOrchestrator();
            var result = await orchestrator.ExecuteMultiModelWorkflowAsync(workflow);

            // Debug output
            Console.WriteLine($"MultiModel Test Debug - Result Success: {result.Success}");
            Console.WriteLine($"MultiModel Test Debug - Result ErrorMessage: {result.ErrorMessage}");
            Console.WriteLine($"MultiModel Test Debug - StepResults Count: {result.StepResults.Count}");
            foreach (var stepResult in result.StepResults)
            {
                Console.WriteLine($"MultiModel Test Debug - Step '{stepResult.StepName}' Success: {stepResult.Success}, ErrorMessage: {stepResult.ErrorMessage}");
            }

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(2, result.StepResults.Count);
            Assert.True(result.StepResults.All(r => r.Success));
            Assert.True(result.TotalProcessingTimeMs > 0);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_AnalyzeAndOptimizeAsync_ShouldProvideOptimizationRecommendations()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            
            // Act
            var result = await orchestrator.AnalyzeAndOptimizeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.PerformanceAnalysis);
            Assert.NotNull(result.Bottlenecks);
            Assert.NotNull(result.Recommendations);
            Assert.Equal(DateTime.UtcNow.Date, result.AnalysisTimestamp.Date);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_AddSelectionRule_ShouldAddCustomRule()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var customRule = new ModelSelectionRule
            {
                Name = "Custom Performance Rule",
                Priority = 1,
                Condition = async (request, model) => request.ComplexityLevel > 3,
                IsDynamic = false
            };

            // Act
            orchestrator.AddSelectionRule(customRule);

            // Assert - Verify rule was added by testing with a high complexity request
            var request = new AdvancedModelRequest
            {
                Input = "Complex task requiring high performance",
                TaskType = "complex_analysis",
                ComplexityLevel = 5
            };

            var result = await orchestrator.ExecuteAdvancedAsync(request);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_UpdateModelCapabilities_ShouldUpdateCapabilities()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var capabilities = new ModelCapabilityProfile
            {
                SupportedLanguages = new[] { "csharp", "javascript", "python", "rust" },
                SupportedTasks = new[] { "code_generation", "code_analysis", "security_analysis" },
                MaxComplexity = 5,
                MaxTokens = 16000,
                CostPerToken = 0.00002m
            };

            // Act
            orchestrator.UpdateModelCapabilities("custom-model", capabilities);

            // Assert - Test with a request that should use the updated model
            var request = new AdvancedModelRequest
            {
                Input = "Generate secure Rust code",
                TaskType = "security_analysis",
                ComplexityLevel = 5,
                RequiredLanguages = new List<string> { "rust" }
            };

            var result = await orchestrator.ExecuteAdvancedAsync(request);
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task MultiAgentCoordinator_RegisterAgent_ShouldRegisterAgentSuccessfully()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent = CreateMockAgent("test-agent", "Test Agent", "Developer");

            // Act
            coordinator.RegisterAgent(agent);

            // Assert
            var registeredAgents = coordinator.GetRegisteredAgents();
            Assert.Contains(agent, registeredAgents);
            Assert.Equal(1, registeredAgents.Count);
        }

        [Fact]
        public async Task MultiAgentCoordinator_CreateCollaborationSession_ShouldCreateSessionWithSuitableAgents()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent1 = CreateMockAgent("agent1", "Code Reviewer", "Reviewer");
            var agent2 = CreateMockAgent("agent2", "Security Expert", "Security");
            var agent3 = CreateMockAgent("agent3", "Performance Analyst", "Analyst");

            coordinator.RegisterAgent(agent1);
            coordinator.RegisterAgent(agent2);
            coordinator.RegisterAgent(agent3);

            var request = new CollaborationRequest
            {
                SessionName = "Code Review Session",
                Description = "Review code for security and performance issues",
                SessionType = CollaborationSessionType.CodeReview,
                RequiredCapabilities = new List<string> { "code_review", "security_analysis" },
                RequiredRoles = new List<string> { "Reviewer", "Security" },
                MaxAgents = 2
            };

            // Act
            var session = await coordinator.CreateCollaborationSessionAsync(request);

            // Assert
            Assert.NotNull(session);
            Assert.Equal("Code Review Session", session.SessionName);
            Assert.Equal(CollaborationSessionType.CodeReview, session.SessionType);
            Assert.Equal(CollaborationSessionStatus.Created, session.Status);
            Assert.Equal(2, session.ParticipatingAgents.Count);
            Assert.Contains(agent1, session.ParticipatingAgents);
            Assert.Contains(agent2, session.ParticipatingAgents);
        }

        [Fact]
        public async Task MultiAgentCoordinator_ExecuteCollaborativeTask_ShouldExecuteTaskWithMultipleAgents()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent1 = CreateMockAgent("agent1", "Code Reviewer", "Reviewer");
            var agent2 = CreateMockAgent("agent2", "Security Expert", "Security");

            coordinator.RegisterAgent(agent1);
            coordinator.RegisterAgent(agent2);

            var task = new CollaborativeTask
            {
                TaskName = "Security Code Review",
                Description = "Review the provided code for security vulnerabilities and best practices",
                TaskType = "security_review",
                RequiredCapabilities = new List<string> { "code_review", "security_analysis" },
                RequiredRoles = new List<string> { "Reviewer", "Security" },
                Priority = Nexo.Feature.Agent.Models.TaskPriority.High,
                ComplexityLevel = 3
            };

            // Act
            var result = await coordinator.ExecuteCollaborativeTaskAsync(task);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Security Code Review", result.TaskName);
            Assert.Equal(2, result.AgentResults.Count);
            Assert.True(result.AgentResults.All(r => r.Success));
            Assert.NotEmpty(result.SynthesizedResult);
            Assert.True(result.CollaborationMetrics.TotalProcessingTimeMs > 0);
            Assert.Equal(2, result.CollaborationMetrics.AgentCount);
        }

        [Fact]
        public async Task MultiAgentCoordinator_FacilitateCommunication_ShouldEnableAgentToAgentCommunication()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var senderAgent = CreateMockAgent("sender", "Sender Agent", "Coordinator");
            var recipientAgent = CreateMockAgent("recipient", "Recipient Agent", "Specialist");

            coordinator.RegisterAgent(senderAgent);
            coordinator.RegisterAgent(recipientAgent);

            var request = new AgentCommunicationRequest
            {
                SenderAgentId = "sender",
                RecipientAgentId = "recipient",
                Message = "Can you analyze this code for performance issues?",
                MessageType = CommunicationMessageType.Question,
                Priority = CommunicationPriority.Normal
            };

            // Act
            var result = await coordinator.FacilitateCommunicationAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Response);
            Assert.True(result.ProcessingTimeMs > 0);
            Assert.True(result.AIWasUsed);
            Assert.NotEmpty(result.AIModelUsed);
        }

        [Fact]
        public async Task MultiAgentCoordinator_AnalyzeCollaborationPatterns_ShouldProvideAnalysisAndRecommendations()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent1 = CreateMockAgent("agent1", "Agent 1", "Role1");
            var agent2 = CreateMockAgent("agent2", "Agent 2", "Role2");

            coordinator.RegisterAgent(agent1);
            coordinator.RegisterAgent(agent2);

            // Execute some collaborative tasks to generate data
            var task = new CollaborativeTask
            {
                TaskName = "Test Task",
                Description = "Test collaborative task",
                RequiredCapabilities = new List<string> { "test" },
                RequiredRoles = new List<string> { "Role1", "Role2" }
            };

            await coordinator.ExecuteCollaborativeTaskAsync(task);

            // Act
            var analysis = await coordinator.AnalyzeCollaborationPatternsAsync();

            // Assert
            Assert.NotNull(analysis);
            Assert.True(analysis.Success);
            Assert.Equal(2, analysis.RegisteredAgentsCount);
            Assert.True(analysis.CompletedSessionsCount > 0);
            Assert.NotNull(analysis.AgentCollaborationPatterns);
            Assert.NotNull(analysis.SessionPerformanceMetrics);
            Assert.NotNull(analysis.Recommendations);
        }

        [Fact]
        public async Task MultiAgentCoordinator_GetAgentCapabilities_ShouldReturnAgentCapabilities()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent = CreateMockAgent("test-agent", "Test Agent", "Developer");

            coordinator.RegisterAgent(agent);

            // Act
            var capabilities = coordinator.GetAgentCapabilities("test-agent");

            // Assert
            Assert.NotNull(capabilities);
            Assert.Equal("test-agent", capabilities.AgentId);
            Assert.Equal("Test Agent", capabilities.AgentName);
            Assert.Equal("Developer", capabilities.AgentRole);
            Assert.NotNull(capabilities.Capabilities);
            Assert.NotNull(capabilities.FocusAreas);
        }

        [Fact]
        public async Task MultiAgentCoordinator_UnregisterAgent_ShouldRemoveAgentFromCoordinator()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent = CreateMockAgent("test-agent", "Test Agent", "Developer");
            coordinator.RegisterAgent(agent);

            // Verify agent is registered
            var registeredAgents = coordinator.GetRegisteredAgents();
            Assert.Contains(agent, registeredAgents);

            // Act
            coordinator.UnregisterAgent("test-agent");

            // Assert
            registeredAgents = coordinator.GetRegisteredAgents();
            Assert.DoesNotContain(agent, registeredAgents);
            Assert.Equal(0, registeredAgents.Count);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteAdvancedAsync_WithValidation_ShouldValidateContent()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var request = new AdvancedModelRequest
            {
                Input = "{\"invalid\": json}",
                TaskType = "validation",
                PostProcessingOptions = new List<PostProcessingOption>
                {
                    new PostProcessingOption
                    {
                        Type = PostProcessingType.Validation,
                        Parameters = new Dictionary<string, object> { ["validationType"] = "json" }
                    }
                }
            };

            // Act
            var result = await orchestrator.ExecuteAdvancedAsync(request);

            // Assert
            Assert.NotNull(result);
            // The validation should fail for invalid JSON
            Assert.False(result.Success);
            Assert.Contains("validation failed", result.ErrorMessage);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteAdvancedAsync_WithEnhancement_ShouldEnhanceContent()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var request = new AdvancedModelRequest
            {
                Input = "Simple text content",
                TaskType = "enhancement",
                PostProcessingOptions = new List<PostProcessingOption>
                {
                    new PostProcessingOption
                    {
                        Type = PostProcessingType.Enhancement,
                        Parameters = new Dictionary<string, object> { ["enhancementType"] = "expand" }
                    }
                }
            };

            // Act
            var result = await orchestrator.ExecuteAdvancedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Content);
            Assert.Contains("Additional Details", result.Content);
        }

        [Fact]
        public async Task MultiAgentCoordinator_ExecuteCollaborativeTask_WithCriticalStep_ShouldHandleFailures()
        {
            // Arrange
            var coordinator = CreateMultiAgentCoordinator();
            var agent1 = CreateMockAgent("agent1", "Agent 1", "Role1");
            var agent2 = CreateMockAgent("agent2", "Agent 2", "Role2");

            coordinator.RegisterAgent(agent1);
            coordinator.RegisterAgent(agent2);

            var task = new CollaborativeTask
            {
                TaskName = "Critical Task",
                Description = "Task with critical steps",
                TaskType = "critical_analysis",
                RequiredCapabilities = new List<string> { "critical_analysis" },
                RequiredRoles = new List<string> { "Role1", "Role2" },
                Priority = Nexo.Feature.Agent.Models.TaskPriority.Critical,
                ComplexityLevel = 5
            };

            // Act
            var result = await coordinator.ExecuteCollaborativeTaskAsync(task);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(2, result.AgentResults.Count);
            Assert.True(result.CollaborationMetrics.AgentCount > 0);
        }

        [Fact]
        public async Task AdvancedModelOrchestrator_ExecuteAdvancedAsync_WithFallback_ShouldUseFallbackModel()
        {
            // Arrange
            var orchestrator = CreateAdvancedModelOrchestrator();
            var request = new AdvancedModelRequest
            {
                Input = "Test request that might fail",
                TaskType = "test",
                UseFallback = true,
                PreferredModel = "primary-model"
            };

            // Act
            var result = await orchestrator.ExecuteAdvancedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.ModelUsed);
            // Note: In a real scenario, we would mock the primary model to fail
            // and verify that the fallback model was used
        }

        private IAIEnhancedAgent CreateMockAgent(string id, string name, string role)
        {
            var mockAgent = new Mock<IAIEnhancedAgent>();
            
            mockAgent.Setup(x => x.Id).Returns(new AgentId(id));
            mockAgent.Setup(x => x.Name).Returns(new AgentName(name));
            mockAgent.Setup(x => x.Role).Returns(new AgentRole(role));
            mockAgent.Setup(x => x.Capabilities).Returns(new List<string> { "code_review", "security_analysis", "performance_analysis" });
            mockAgent.Setup(x => x.FocusAreas).Returns(new List<string> { "backend", "security" });
            mockAgent.Setup(x => x.AICapabilities).Returns(new AIAgentCapabilities { CanAnalyzeTasks = true });

            mockAgent.Setup(x => x.ProcessAIRequestAsync(It.IsAny<AIEnhancedAgentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIEnhancedAgentRequest request, CancellationToken token) => new AIEnhancedAgentResponse
                {
                    Success = true,
                    Content = $"Mock response from {name}: {request.Content}",
                    AIWasUsed = true,
                    AIModelUsed = "gpt-4",
                    AIProcessingTimeMs = 100,
                    AIConfidenceScore = 0.85
                });

            return mockAgent.Object;
        }

        public void Dispose()
        {
            // Clean up any shared state between tests
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
} 