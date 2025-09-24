using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Nexo.Agent.Contracts;

namespace NexoDoomGame.Validators
{
    public class ScriptValidator
    {
        private readonly ITaskExecutionAgent _nexoAgent;

        public ScriptValidator(ITaskExecutionAgent nexoAgent)
        {
            _nexoAgent = nexoAgent ?? throw new ArgumentNullException(nameof(nexoAgent));
        }

        public async Task<ValidationResult> ValidateScriptsAsync(float threshold)
        {
            Debug.Log("🔍 Validating script quality...");
            
            try
            {
                var scripts = FindAllScripts();
                var issues = new List<ValidationIssue>();
                var totalScore = 0f;
                var maxScore = scripts.Count * 100f;

                foreach (var script in scripts)
                {
                    var scriptScore = await ValidateSingleScript(script);
                    totalScore += scriptScore;
                    
                    if (scriptScore < threshold * 100f)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = "Script Quality",
                            Description = $"Script {script.name} has quality issues",
                            Severity = scriptScore < 50f ? "High" : "Medium",
                            Script = script
                        });
                    }
                }

                var finalScore = maxScore > 0 ? totalScore / maxScore : 0f;
                
                return new ValidationResult
                {
                    Phase = "Script Quality",
                    Score = finalScore,
                    Threshold = threshold,
                    Passed = finalScore >= threshold,
                    Issues = issues,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Script validation error: {ex.Message}");
                return new ValidationResult
                {
                    Phase = "Script Quality",
                    Score = 0f,
                    Threshold = threshold,
                    Passed = false,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = "Script Validation Error",
                            Description = ex.Message,
                            Severity = "High"
                        }
                    },
                    Timestamp = DateTime.Now
                };
            }
        }

        public async Task ImproveScriptsAsync(ValidationResult result)
        {
            Debug.Log($"🔄 Improving scripts based on validation results...");
            
            try
            {
                var improvementTasks = result.Issues
                    .Where(issue => issue.Script != null)
                    .Select(issue => ImproveSingleScript(issue.Script, issue))
                    .ToArray();

                await Task.WhenAll(improvementTasks);
                
                Debug.Log("✅ Script improvements completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Script improvement error: {ex.Message}");
            }
        }

        private List<MonoBehaviour> FindAllScripts()
        {
            return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().ToList();
        }

        private async Task<float> ValidateSingleScript(MonoBehaviour script)
        {
            await Task.Delay(100); // Simulate validation time
            
            var score = 100f;
            
            // Check for common script issues
            if (script == null) return 0f;
            
            var scriptType = script.GetType();
            
            // Check for missing components
            if (scriptType.GetCustomAttributes(typeof(RequireComponent), true).Length > 0)
            {
                var requiredComponents = scriptType.GetCustomAttributes(typeof(RequireComponent), true)
                    .Cast<RequireComponent>();
                
                foreach (var req in requiredComponents)
                {
                    if (script.GetComponent(req.m_Type0) == null)
                    {
                        score -= 20f;
                    }
                }
            }
            
            // Check for performance issues
            if (scriptType.GetMethod("Update") != null)
            {
                score -= 10f; // Update methods can be performance issues
            }
            
            // Check for proper initialization
            if (scriptType.GetMethod("Start") == null && scriptType.GetMethod("Awake") == null)
            {
                score -= 5f;
            }
            
            return Math.Max(0f, score);
        }

        private async Task ImproveSingleScript(MonoBehaviour script, ValidationIssue issue)
        {
            Debug.Log($"🔧 Improving script: {script.name}");
            
            try
            {
                // Create improvement task for Nexo Agent
                var improvementTask = new Nexo.Agent.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Improve Script: {script.name}",
                    Description = $"Fix quality issues in {script.name}: {issue.Description}",
                    Priority = issue.Severity == "High" ? 1 : 2,
                    Status = "Pending"
                };
                
                await _nexoAgent.ExecuteTaskAsync(improvementTask);
                
                Debug.Log($"✅ Script improvement completed: {script.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to improve script {script.name}: {ex.Message}");
            }
        }
    }
}
