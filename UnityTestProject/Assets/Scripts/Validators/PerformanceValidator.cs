using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Nexo.Agent.Contracts;

namespace NexoDoomGame.Validators
{
    public class PerformanceValidator
    {
        private readonly ITaskExecutionAgent _nexoAgent;

        public PerformanceValidator(ITaskExecutionAgent nexoAgent)
        {
            _nexoAgent = nexoAgent ?? throw new ArgumentNullException(nameof(nexoAgent));
        }

        public async Task<ValidationResult> ValidatePerformanceAsync(float threshold)
        {
            Debug.Log("🔍 Validating performance...");
            
            try
            {
                var performanceMetrics = await CollectPerformanceMetrics();
                var issues = new List<ValidationIssue>();
                var totalScore = 0f;
                var maxScore = performanceMetrics.Count * 100f;

                foreach (var metric in performanceMetrics)
                {
                    var metricScore = EvaluatePerformanceMetric(metric);
                    totalScore += metricScore;
                    
                    if (metricScore < threshold * 100f)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = "Performance",
                            Description = $"Performance issue: {metric.Name} - {metric.Value}",
                            Severity = metricScore < 50f ? "High" : "Medium"
                        });
                    }
                }

                var finalScore = maxScore > 0 ? totalScore / maxScore : 0f;
                
                return new ValidationResult
                {
                    Phase = "Performance",
                    Score = finalScore,
                    Threshold = threshold,
                    Passed = finalScore >= threshold,
                    Issues = issues,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Performance validation error: {ex.Message}");
                return new ValidationResult
                {
                    Phase = "Performance",
                    Score = 0f,
                    Threshold = threshold,
                    Passed = false,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = "Performance Validation Error",
                            Description = ex.Message,
                            Severity = "High"
                        }
                    },
                    Timestamp = DateTime.Now
                };
            }
        }

        public async Task ImprovePerformanceAsync(ValidationResult result)
        {
            Debug.Log($"🔄 Improving performance based on validation results...");
            
            try
            {
                var improvementTasks = result.Issues
                    .Select(issue => ImprovePerformanceIssue(issue))
                    .ToArray();

                await Task.WhenAll(improvementTasks);
                
                Debug.Log("✅ Performance improvements completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Performance improvement error: {ex.Message}");
            }
        }

        private async Task<List<PerformanceMetric>> CollectPerformanceMetrics()
        {
            var metrics = new List<PerformanceMetric>();
            
            // Frame rate
            metrics.Add(new PerformanceMetric
            {
                Name = "Frame Rate",
                Value = 1f / Time.deltaTime,
                Unit = "FPS",
                Threshold = 60f
            });
            
            // Memory usage
            metrics.Add(new PerformanceMetric
            {
                Name = "Memory Usage",
                Value = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / 1024f / 1024f,
                Unit = "MB",
                Threshold = 100f
            });
            
            // Draw calls
            metrics.Add(new PerformanceMetric
            {
                Name = "Draw Calls",
                Value = UnityEngine.Profiling.Profiler.GetCounterValue("Draw Calls"),
                Unit = "Count",
                Threshold = 100f
            });
            
            // Triangles
            metrics.Add(new PerformanceMetric
            {
                Name = "Triangles",
                Value = UnityEngine.Profiling.Profiler.GetCounterValue("Triangles"),
                Unit = "Count",
                Threshold = 100000f
            });
            
            await Task.Delay(100); // Simulate collection time
            
            return metrics;
        }

        private float EvaluatePerformanceMetric(PerformanceMetric metric)
        {
            var score = 100f;
            
            // Evaluate based on threshold
            if (metric.Value > metric.Threshold)
            {
                var excess = (metric.Value - metric.Threshold) / metric.Threshold;
                score -= Math.Min(50f, excess * 100f);
            }
            
            // Specific metric evaluations
            switch (metric.Name)
            {
                case "Frame Rate":
                    if (metric.Value < 30f) score = 0f;
                    else if (metric.Value < 60f) score = Math.Min(score, 70f);
                    break;
                    
                case "Memory Usage":
                    if (metric.Value > 500f) score = 0f;
                    else if (metric.Value > 200f) score = Math.Min(score, 60f);
                    break;
                    
                case "Draw Calls":
                    if (metric.Value > 500f) score = 0f;
                    else if (metric.Value > 200f) score = Math.Min(score, 70f);
                    break;
                    
                case "Triangles":
                    if (metric.Value > 500000f) score = 0f;
                    else if (metric.Value > 200000f) score = Math.Min(score, 80f);
                    break;
            }
            
            return Math.Max(0f, score);
        }

        private async Task ImprovePerformanceIssue(ValidationIssue issue)
        {
            Debug.Log($"🔧 Improving performance issue: {issue.Description}");
            
            try
            {
                // Create improvement task for Nexo Agent
                var improvementTask = new Nexo.Agent.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Improve Performance: {issue.Type}",
                    Description = $"Fix performance issue: {issue.Description}",
                    Priority = issue.Severity == "High" ? 1 : 2,
                    Status = "Pending"
                };
                
                await _nexoAgent.ExecuteTaskAsync(improvementTask);
                
                Debug.Log($"✅ Performance improvement completed: {issue.Type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to improve performance issue {issue.Type}: {ex.Message}");
            }
        }
    }

    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public float Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public float Threshold { get; set; }
    }
}
