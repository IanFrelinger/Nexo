using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Workflows
{
    /// <summary>
    /// Reporting functionality for game development workflow.
    /// </summary>
    public partial class GameDevelopmentWorkflow
    {
        private async Task<GameDevelopmentReport> GenerateGameDevelopmentReport(
            WorkflowResult workflowResult, 
            GameDevelopmentWorkflowRequest request)
        {
            var report = new GameDevelopmentReport
            {
                ProjectPath = request.ProjectPath,
                WorkflowId = workflowResult.WorkflowId,
                StartTime = workflowResult.StartTime,
                EndTime = workflowResult.EndTime,
                Status = workflowResult.Status,
                Summary = GenerateWorkflowSummary(workflowResult),
                Recommendations = GenerateRecommendations(workflowResult),
                NextSteps = GenerateNextSteps(workflowResult, request)
            };
            
            return report;
        }
        
        private string GenerateWorkflowSummary(WorkflowResult workflowResult)
        {
            var summary = $"Game Development Workflow Summary:\n";
            summary += $"Status: {workflowResult.Status}\n";
            summary += $"Duration: {workflowResult.EndTime - workflowResult.StartTime}\n";
            summary += $"Steps Completed: {workflowResult.Steps.Count()}\n";
            
            if (workflowResult.Steps.ContainsKey("ProjectAnalysis"))
            {
                var analysis = workflowResult.Steps["ProjectAnalysis"] as UnityProjectAnalysis;
                summary += $"Scripts Analyzed: {analysis?.ScriptAnalysis.Scripts.Count() ?? 0}\n";
                summary += $"Scenes Analyzed: {analysis?.SceneAnalysis.Scenes.Count() ?? 0}\n";
                summary += $"Assets Analyzed: {analysis?.AssetAnalysis.Assets.Count() ?? 0}\n";
            }
            
            if (workflowResult.Steps.ContainsKey("PerformanceOptimization"))
            {
                var optimizations = workflowResult.Steps["PerformanceOptimization"] as GamePerformanceOptimizations;
                summary += $"Iteration Optimizations: {optimizations?.IterationOptimizations.Count() ?? 0}\n";
                summary += $"Asset Optimizations: {optimizations?.AssetOptimizations.Count() ?? 0}\n";
                summary += $"Rendering Optimizations: {optimizations?.RenderingOptimizations.Count() ?? 0}\n";
            }
            
            return summary;
        }
        
        private IEnumerable<string> GenerateRecommendations(WorkflowResult workflowResult)
        {
            var recommendations = new List<string>();
            
            if (workflowResult.Steps.ContainsKey("ProjectAnalysis"))
            {
                var analysis = workflowResult.Steps["ProjectAnalysis"] as UnityProjectAnalysis;
                if (analysis?.IterationOptimizations.Any() == true)
                {
                    recommendations.Add("Apply iteration pattern optimizations to improve performance");
                }
                
                if (analysis?.AssetAnalysis.OptimizableAssetSize > 0)
                {
                    recommendations.Add("Optimize asset sizes to reduce build size and improve loading times");
                }
            }
            
            if (workflowResult.Steps.ContainsKey("BalanceAnalysis"))
            {
                recommendations.Add("Review and implement balance recommendations for better gameplay");
            }
            
            if (workflowResult.Steps.ContainsKey("BuildOptimization"))
            {
                recommendations.Add("Apply build optimizations for target platforms");
            }
            
            return recommendations;
        }
        
        private IEnumerable<string> GenerateNextSteps(WorkflowResult workflowResult, GameDevelopmentWorkflowRequest request)
        {
            var nextSteps = new List<string>();
            
            if (workflowResult.Status == WorkflowStatus.Completed)
            {
                nextSteps.Add("Review generated recommendations and implement changes");
                nextSteps.Add("Test optimized code and assets in target environments");
                nextSteps.Add("Run performance profiling to validate improvements");
                
                if (request.OptimizeBuilds)
                {
                    nextSteps.Add("Build and test optimized versions for all target platforms");
                }
                
                nextSteps.Add("Schedule follow-up optimization workflow in 2-4 weeks");
            }
            else if (workflowResult.Status == WorkflowStatus.Failed)
            {
                nextSteps.Add("Review error logs and fix issues");
                nextSteps.Add("Re-run workflow with corrected parameters");
                nextSteps.Add("Contact support if issues persist");
            }
            
            return nextSteps;
        }
    }
}
