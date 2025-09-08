using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.Unity.Monitoring;

namespace Nexo.Feature.Unity.Tests
{
    public class UnityFeatureTests
    {
        private readonly IHost _host;

        public UnityFeatureTests()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add Unity feature services
                    services.AddUnityFeature();
                    
                    // Mock dependencies
                    services.AddSingleton(new Mock<ILogger<UnityFeatureTests>>().Object);
                })
                .Build();
        }

        [Fact]
        public async Task UnityProjectAnalyzer_ShouldAnalyzeProject()
        {
            // Arrange
            var analyzer = _host.Services.GetRequiredService<IUnityProjectAnalyzer>();
            var projectPath = "test-project";

            // Act
            var analysis = await analyzer.AnalyzeProjectAsync(projectPath);

            // Assert
            Assert.NotNull(analysis);
            Assert.Equal(projectPath, analysis.ProjectPath);
        }

        [Fact]
        public async Task GameplayBalanceAgent_ShouldProcessRequest()
        {
            // Arrange
            var agent = _host.Services.GetRequiredService<GameplayBalanceAgent>();
            var request = new AgentRequest
            {
                Input = "Analyze game balance",
                Context = new AgentContext()
            };

            // Act
            var response = await agent.ProcessAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.HasResult);
        }

        [Fact]
        public async Task GameDevelopmentWorkflow_ShouldExecute()
        {
            // Arrange
            var workflow = _host.Services.GetRequiredService<GameDevelopmentWorkflow>();
            var request = new GameDevelopmentWorkflowRequest
            {
                ProjectPath = "test-project",
                GenerateNewMechanics = true,
                AnalyzeBalance = true,
                OptimizeBuilds = true
            };

            // Act
            var result = await workflow.ExecuteAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(WorkflowStatus.Completed, result.Status);
        }

        [Fact]
        public async Task GamePerformanceMonitor_ShouldStartMonitoring()
        {
            // Arrange
            var monitor = _host.Services.GetRequiredService<IGamePerformanceMonitor>();
            var config = new GameMonitoringConfiguration
            {
                GameName = "Test Game",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                MaxHistorySize = 100
            };

            // Act & Assert
            await monitor.StartMonitoringAsync(config);
            await monitor.StopMonitoringAsync();
        }
    }
}
