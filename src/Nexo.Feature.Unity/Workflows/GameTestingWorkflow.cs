using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;
using Nexo.Feature.Unity.Workflows;

namespace Nexo.Feature.Unity.Workflows
{
    /// <summary>
    /// Automated game testing and quality assurance workflow
    /// </summary>
    public class GameTestingWorkflow : IWorkflow
    {
        private readonly IUnityTestRunner _testRunner;
        private readonly IGameplayTester _gameplayTester;
        private readonly IPerformanceTester _performanceTester;
        private readonly IBalanceTester _balanceTester;
        private readonly ILogger<GameTestingWorkflow> _logger;
        
        public GameTestingWorkflow(
            IUnityTestRunner testRunner,
            IGameplayTester gameplayTester,
            IPerformanceTester performanceTester,
            IBalanceTester balanceTester,
            ILogger<GameTestingWorkflow> logger)
        {
            _testRunner = testRunner;
            _gameplayTester = gameplayTester;
            _performanceTester = performanceTester;
            _balanceTester = balanceTester;
            _logger = logger;
        }
        
        public async Task<WorkflowResult> ExecuteAsync(GameTestingWorkflowRequest request)
        {
            _logger.LogInformation("Starting game testing workflow for project: {ProjectPath}", request.ProjectPath);
            
            var workflowResult = new WorkflowResult
            {
                WorkflowId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                Status = WorkflowStatus.Running
            };
            
            try
            {
                // Phase 1: Unit Testing
                _logger.LogInformation("Phase 1: Running Unity unit tests");
                var unitTestResults = await _testRunner.RunUnityTestsAsync(request.ProjectPath);
                workflowResult.AddStep("UnitTesting", unitTestResults);
                
                // Phase 2: Performance Testing
                _logger.LogInformation("Phase 2: Running performance tests");
                var performanceResults = await _performanceTester.RunPerformanceTestsAsync(new PerformanceTestRequest
                {
                    ProjectPath = request.ProjectPath,
                    TestScenes = request.TestScenes,
                    TargetFrameRate = request.TargetFrameRate,
                    TestDuration = request.TestDuration
                });
                workflowResult.AddStep("PerformanceTesting", performanceResults);
                
                // Phase 3: Gameplay Testing
                if (request.RunGameplayTests)
                {
                    _logger.LogInformation("Phase 3: Running gameplay tests");
                    var gameplayResults = await _gameplayTester.RunGameplayTestsAsync(new GameplayTestRequest
                    {
                        ProjectPath = request.ProjectPath,
                        TestScenarios = request.GameplayScenarios,
                        PlayerProfiles = request.TestPlayerProfiles
                    });
                    workflowResult.AddStep("GameplayTesting", gameplayResults);
                }
                
                // Phase 4: Balance Testing
                if (request.TestBalance)
                {
                    _logger.LogInformation("Phase 4: Running balance tests");
                    var balanceResults = await _balanceTester.RunBalanceTestsAsync(new BalanceTestRequest
                    {
                        ProjectPath = request.ProjectPath,
                        BalanceScenarios = request.BalanceScenarios,
                        SimulationCount = request.BalanceSimulationCount
                    });
                    workflowResult.AddStep("BalanceTesting", balanceResults);
                }
                
                // Phase 5: Generate Test Report
                _logger.LogInformation("Phase 5: Generating test report");
                var testReport = await GenerateTestReport(workflowResult, request);
                workflowResult.FinalReport = testReport;
                
                workflowResult.Status = WorkflowStatus.Completed;
                workflowResult.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation("Game testing workflow completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Game testing workflow failed");
                workflowResult.Status = WorkflowStatus.Failed;
                workflowResult.ErrorMessage = ex.Message;
                workflowResult.EndTime = DateTime.UtcNow;
            }
            
            return workflowResult;
        }
        
        private async Task<GameTestReport> GenerateTestReport(WorkflowResult workflowResult, GameTestingWorkflowRequest request)
        {
            var report = new GameTestReport
            {
                ProjectPath = request.ProjectPath,
                WorkflowId = workflowResult.WorkflowId,
                StartTime = workflowResult.StartTime,
                EndTime = workflowResult.EndTime,
                Status = workflowResult.Status,
                TestSummary = GenerateTestSummary(workflowResult),
                TestResults = ExtractTestResults(workflowResult),
                QualityMetrics = CalculateQualityMetrics(workflowResult),
                Recommendations = GenerateTestRecommendations(workflowResult),
                NextSteps = GenerateTestNextSteps(workflowResult, request)
            };
            
            return report;
        }
        
        private string GenerateTestSummary(WorkflowResult workflowResult)
        {
            var summary = $"Game Testing Workflow Summary:\n";
            summary += $"Status: {workflowResult.Status}\n";
            summary += $"Duration: {workflowResult.EndTime - workflowResult.StartTime}\n";
            summary += $"Test Phases Completed: {workflowResult.Steps.Count()}\n";
            
            if (workflowResult.Steps.ContainsKey("UnitTesting"))
            {
                var unitTests = workflowResult.Steps["UnitTesting"] as UnityTestResults;
                summary += $"Unit Tests: {unitTests?.PassedTests ?? 0}/{unitTests?.TotalTests ?? 0} passed\n";
            }
            
            if (workflowResult.Steps.ContainsKey("PerformanceTesting"))
            {
                var perfTests = workflowResult.Steps["PerformanceTesting"] as PerformanceTestResults;
                summary += $"Performance Tests: {perfTests?.PassedTests ?? 0}/{perfTests?.TotalTests ?? 0} passed\n";
                summary += $"Average FPS: {perfTests?.AverageFrameRate:F1}\n";
            }
            
            if (workflowResult.Steps.ContainsKey("GameplayTesting"))
            {
                var gameplayTests = workflowResult.Steps["GameplayTesting"] as GameplayTestResults;
                summary += $"Gameplay Tests: {gameplayTests?.PassedTests ?? 0}/{gameplayTests?.TotalTests ?? 0} passed\n";
            }
            
            if (workflowResult.Steps.ContainsKey("BalanceTesting"))
            {
                var balanceTests = workflowResult.Steps["BalanceTesting"] as BalanceTestResults;
                summary += $"Balance Tests: {balanceTests?.PassedTests ?? 0}/{balanceTests?.TotalTests ?? 0} passed\n";
            }
            
            return summary;
        }
        
        private Dictionary<string, object> ExtractTestResults(WorkflowResult workflowResult)
        {
            var results = new Dictionary<string, object>();
            
            foreach (var step in workflowResult.Steps)
            {
                results[step.Key] = step.Value;
            }
            
            return results;
        }
        
        private QualityMetrics CalculateQualityMetrics(WorkflowResult workflowResult)
        {
            var metrics = new QualityMetrics();
            
            // Calculate overall test pass rate
            var totalTests = 0;
            var passedTests = 0;
            
            if (workflowResult.Steps.ContainsKey("UnitTesting"))
            {
                var unitTests = workflowResult.Steps["UnitTesting"] as UnityTestResults;
                totalTests += unitTests?.TotalTests ?? 0;
                passedTests += unitTests?.PassedTests ?? 0;
            }
            
            if (workflowResult.Steps.ContainsKey("PerformanceTesting"))
            {
                var perfTests = workflowResult.Steps["PerformanceTesting"] as PerformanceTestResults;
                totalTests += perfTests?.TotalTests ?? 0;
                passedTests += perfTests?.PassedTests ?? 0;
            }
            
            if (workflowResult.Steps.ContainsKey("GameplayTesting"))
            {
                var gameplayTests = workflowResult.Steps["GameplayTesting"] as GameplayTestResults;
                totalTests += gameplayTests?.TotalTests ?? 0;
                passedTests += gameplayTests?.PassedTests ?? 0;
            }
            
            if (workflowResult.Steps.ContainsKey("BalanceTesting"))
            {
                var balanceTests = workflowResult.Steps["BalanceTesting"] as BalanceTestResults;
                totalTests += balanceTests?.TotalTests ?? 0;
                passedTests += balanceTests?.PassedTests ?? 0;
            }
            
            metrics.OverallTestPassRate = totalTests > 0 ? (double)passedTests / totalTests : 0;
            metrics.TotalTests = totalTests;
            metrics.PassedTests = passedTests;
            metrics.FailedTests = totalTests - passedTests;
            
            // Calculate performance metrics
            if (workflowResult.Steps.ContainsKey("PerformanceTesting"))
            {
                var perfTests = workflowResult.Steps["PerformanceTesting"] as PerformanceTestResults;
                metrics.AverageFrameRate = perfTests?.AverageFrameRate ?? 0;
                metrics.MinFrameRate = perfTests?.MinFrameRate ?? 0;
                metrics.PerformanceScore = CalculatePerformanceScore(perfTests);
            }
            
            // Calculate quality score
            metrics.QualityScore = CalculateOverallQualityScore(metrics);
            
            return metrics;
        }
        
        private double CalculatePerformanceScore(PerformanceTestResults? perfTests)
        {
            if (perfTests == null) return 0;
            
            var frameRateScore = Math.Min(perfTests.AverageFrameRate / 60.0, 1.0); // Normalize to 60 FPS
            var stabilityScore = 1.0 - (perfTests.FrameRateVariance / 100.0); // Lower variance is better
            
            return (frameRateScore + stabilityScore) / 2.0;
        }
        
        private double CalculateOverallQualityScore(QualityMetrics metrics)
        {
            var testScore = metrics.OverallTestPassRate;
            var performanceScore = metrics.PerformanceScore;
            
            return (testScore + performanceScore) / 2.0;
        }
        
        private IEnumerable<string> GenerateTestRecommendations(WorkflowResult workflowResult)
        {
            var recommendations = new List<string>();
            
            if (workflowResult.Steps.ContainsKey("UnitTesting"))
            {
                var unitTests = workflowResult.Steps["UnitTesting"] as UnityTestResults;
                if (unitTests?.FailedTests > 0)
                {
                    recommendations.Add("Fix failing unit tests to ensure code quality");
                }
            }
            
            if (workflowResult.Steps.ContainsKey("PerformanceTesting"))
            {
                var perfTests = workflowResult.Steps["PerformanceTesting"] as PerformanceTestResults;
                if (perfTests?.AverageFrameRate < 30)
                {
                    recommendations.Add("Optimize performance to achieve target frame rate");
                }
                
                if (perfTests?.FrameRateVariance > 10)
                {
                    recommendations.Add("Improve frame rate stability for consistent gameplay");
                }
            }
            
            if (workflowResult.Steps.ContainsKey("GameplayTesting"))
            {
                var gameplayTests = workflowResult.Steps["GameplayTesting"] as GameplayTestResults;
                if (gameplayTests?.FailedTests > 0)
                {
                    recommendations.Add("Address gameplay issues identified in testing");
                }
            }
            
            if (workflowResult.Steps.ContainsKey("BalanceTesting"))
            {
                var balanceTests = workflowResult.Steps["BalanceTesting"] as BalanceTestResults;
                if (balanceTests?.FailedTests > 0)
                {
                    recommendations.Add("Review and adjust game balance based on test results");
                }
            }
            
            return recommendations;
        }
        
        private IEnumerable<string> GenerateTestNextSteps(WorkflowResult workflowResult, GameTestingWorkflowRequest request)
        {
            var nextSteps = new List<string>();
            
            if (workflowResult.Status == WorkflowStatus.Completed)
            {
                nextSteps.Add("Review test results and address any failures");
                nextSteps.Add("Implement recommended improvements");
                nextSteps.Add("Schedule follow-up testing after fixes");
                
                if (request.RunGameplayTests)
                {
                    nextSteps.Add("Conduct additional gameplay testing with real players");
                }
                
                if (request.TestBalance)
                {
                    nextSteps.Add("Run additional balance simulations with updated parameters");
                }
                
                nextSteps.Add("Prepare for release candidate testing");
            }
            else if (workflowResult.Status == WorkflowStatus.Failed)
            {
                nextSteps.Add("Review error logs and fix critical issues");
                nextSteps.Add("Re-run testing workflow with corrected parameters");
                nextSteps.Add("Contact support if issues persist");
            }
            
            return nextSteps;
        }
    }
    
    /// <summary>
    /// Game testing workflow request
    /// </summary>
    public class GameTestingWorkflowRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public bool RunGameplayTests { get; set; }
        public bool TestBalance { get; set; }
        public IEnumerable<string> TestScenes { get; set; } = new List<string>();
        public double TargetFrameRate { get; set; } = 60.0;
        public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(5);
        public IEnumerable<GameplayTestScenario> GameplayScenarios { get; set; } = new List<GameplayTestScenario>();
        public IEnumerable<PlayerProfile> TestPlayerProfiles { get; set; } = new List<PlayerProfile>();
        public IEnumerable<BalanceTestScenario> BalanceScenarios { get; set; } = new List<BalanceTestScenario>();
        public int BalanceSimulationCount { get; set; } = 1000;
    }
    
    /// <summary>
    /// Game test report
    /// </summary>
    public class GameTestReport
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string WorkflowId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public WorkflowStatus Status { get; set; }
        public string TestSummary { get; set; } = string.Empty;
        public Dictionary<string, object> TestResults { get; set; } = new();
        public QualityMetrics QualityMetrics { get; set; } = new();
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
        public IEnumerable<string> NextSteps { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Quality metrics
    /// </summary>
    public class QualityMetrics
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double OverallTestPassRate { get; set; }
        public double AverageFrameRate { get; set; }
        public double MinFrameRate { get; set; }
        public double PerformanceScore { get; set; }
        public double QualityScore { get; set; }
    }
    
    /// <summary>
    /// Unity test runner interface
    /// </summary>
    public interface IUnityTestRunner
    {
        Task<UnityTestResults> RunUnityTestsAsync(string projectPath);
    }
    
    /// <summary>
    /// Gameplay tester interface
    /// </summary>
    public interface IGameplayTester
    {
        Task<GameplayTestResults> RunGameplayTestsAsync(GameplayTestRequest request);
    }
    
    /// <summary>
    /// Performance tester interface
    /// </summary>
    public interface IPerformanceTester
    {
        Task<PerformanceTestResults> RunPerformanceTestsAsync(PerformanceTestRequest request);
    }
    
    /// <summary>
    /// Balance tester interface
    /// </summary>
    public interface IBalanceTester
    {
        Task<BalanceTestResults> RunBalanceTestsAsync(BalanceTestRequest request);
    }
    
    /// <summary>
    /// Unity test results
    /// </summary>
    public class UnityTestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public IEnumerable<TestFailure> Failures { get; set; } = new List<TestFailure>();
        public TimeSpan ExecutionTime { get; set; }
    }
    
    /// <summary>
    /// Gameplay test request
    /// </summary>
    public class GameplayTestRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public IEnumerable<GameplayTestScenario> TestScenarios { get; set; } = new List<GameplayTestScenario>();
        public IEnumerable<PlayerProfile> PlayerProfiles { get; set; } = new List<PlayerProfile>();
    }
    
    /// <summary>
    /// Gameplay test results
    /// </summary>
    public class GameplayTestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public IEnumerable<GameplayTestFailure> Failures { get; set; } = new List<GameplayTestFailure>();
        public TimeSpan ExecutionTime { get; set; }
    }
    
    /// <summary>
    /// Performance test request
    /// </summary>
    public class PerformanceTestRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public IEnumerable<string> TestScenes { get; set; } = new List<string>();
        public double TargetFrameRate { get; set; } = 60.0;
        public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(5);
    }
    
    /// <summary>
    /// Performance test results
    /// </summary>
    public class PerformanceTestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double AverageFrameRate { get; set; }
        public double MinFrameRate { get; set; }
        public double MaxFrameRate { get; set; }
        public double FrameRateVariance { get; set; }
        public IEnumerable<PerformanceIssue> Issues { get; set; } = new List<PerformanceIssue>();
        public TimeSpan ExecutionTime { get; set; }
    }
    
    /// <summary>
    /// Balance test request
    /// </summary>
    public class BalanceTestRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public IEnumerable<BalanceTestScenario> BalanceScenarios { get; set; } = new List<BalanceTestScenario>();
        public int SimulationCount { get; set; } = 1000;
    }
    
    /// <summary>
    /// Balance test results
    /// </summary>
    public class BalanceTestResults
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double OverallBalanceScore { get; set; }
        public IEnumerable<BalanceIssue> Issues { get; set; } = new List<BalanceIssue>();
        public TimeSpan ExecutionTime { get; set; }
    }
    
    /// <summary>
    /// Test failure
    /// </summary>
    public class TestFailure
    {
        public string TestName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Gameplay test failure
    /// </summary>
    public class GameplayTestFailure
    {
        public string ScenarioName { get; set; } = string.Empty;
        public string PlayerProfile { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ExpectedBehavior { get; set; } = string.Empty;
        public string ActualBehavior { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Gameplay test scenario
    /// </summary>
    public class GameplayTestScenario
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public IEnumerable<string> TestSteps { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Player profile
    /// </summary>
    public class PlayerProfile
    {
        public string Name { get; set; } = string.Empty;
        public string SkillLevel { get; set; } = string.Empty;
        public string PlayStyle { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    /// <summary>
    /// Balance test scenario
    /// </summary>
    public class BalanceTestScenario
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GameMode { get; set; } = string.Empty;
        public IEnumerable<string> TestConditions { get; set; } = new List<string>();
    }
}
