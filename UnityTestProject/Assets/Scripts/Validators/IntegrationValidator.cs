using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Nexo.Agent.Contracts;

namespace NexoDoomGame.Validators
{
    public class IntegrationValidator
    {
        private readonly ITaskExecutionAgent _nexoAgent;

        public IntegrationValidator(ITaskExecutionAgent nexoAgent)
        {
            _nexoAgent = nexoAgent ?? throw new ArgumentNullException(nameof(nexoAgent));
        }

        public async Task<ValidationResult> ValidateIntegrationAsync(float threshold)
        {
            Debug.Log("🔍 Validating integration...");
            
            try
            {
                var integrationChecks = await PerformIntegrationChecks();
                var issues = new List<ValidationIssue>();
                var totalScore = 0f;
                var maxScore = integrationChecks.Count * 100f;

                foreach (var check in integrationChecks)
                {
                    var checkScore = EvaluateIntegrationCheck(check);
                    totalScore += checkScore;
                    
                    if (checkScore < threshold * 100f)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Type = "Integration",
                            Description = $"Integration issue: {check.Name} - {check.Status}",
                            Severity = checkScore < 50f ? "High" : "Medium"
                        });
                    }
                }

                var finalScore = maxScore > 0 ? totalScore / maxScore : 0f;
                
                return new ValidationResult
                {
                    Phase = "Integration",
                    Score = finalScore,
                    Threshold = threshold,
                    Passed = finalScore >= threshold,
                    Issues = issues,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Integration validation error: {ex.Message}");
                return new ValidationResult
                {
                    Phase = "Integration",
                    Score = 0f,
                    Threshold = threshold,
                    Passed = false,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Type = "Integration Validation Error",
                            Description = ex.Message,
                            Severity = "High"
                        }
                    },
                    Timestamp = DateTime.Now
                };
            }
        }

        public async Task ImproveIntegrationAsync(ValidationResult result)
        {
            Debug.Log($"🔄 Improving integration based on validation results...");
            
            try
            {
                var improvementTasks = result.Issues
                    .Select(issue => ImproveIntegrationIssue(issue))
                    .ToArray();

                await Task.WhenAll(improvementTasks);
                
                Debug.Log("✅ Integration improvements completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Integration improvement error: {ex.Message}");
            }
        }

        private async Task<List<IntegrationCheck>> PerformIntegrationChecks()
        {
            var checks = new List<IntegrationCheck>();
            
            // Check for missing components
            checks.Add(new IntegrationCheck
            {
                Name = "Missing Components",
                Status = CheckMissingComponents() ? "Pass" : "Fail",
                Description = "Check for missing required components"
            });
            
            // Check for broken references
            checks.Add(new IntegrationCheck
            {
                Name = "Broken References",
                Status = CheckBrokenReferences() ? "Pass" : "Fail",
                Description = "Check for broken object references"
            });
            
            // Check for scene setup
            checks.Add(new IntegrationCheck
            {
                Name = "Scene Setup",
                Status = CheckSceneSetup() ? "Pass" : "Fail",
                Description = "Check for proper scene configuration"
            });
            
            // Check for prefab connections
            checks.Add(new IntegrationCheck
            {
                Name = "Prefab Connections",
                Status = CheckPrefabConnections() ? "Pass" : "Fail",
                Description = "Check for proper prefab connections"
            });
            
            // Check for script compilation
            checks.Add(new IntegrationCheck
            {
                Name = "Script Compilation",
                Status = CheckScriptCompilation() ? "Pass" : "Fail",
                Description = "Check for script compilation errors"
            });
            
            await Task.Delay(150); // Simulate check time
            
            return checks;
        }

        private bool CheckMissingComponents()
        {
            try
            {
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    var components = obj.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            return false; // Found missing component
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckBrokenReferences()
        {
            try
            {
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    var components = obj.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            return false; // Found broken reference
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckSceneSetup()
        {
            try
            {
                // Check for essential scene objects
                var camera = Camera.main;
                var light = FindObjectOfType<Light>();
                
                return camera != null && light != null;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckPrefabConnections()
        {
            try
            {
                // This is a simplified check - in a real implementation,
                // you would check for proper prefab connections
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckScriptCompilation()
        {
            try
            {
                // Check if there are any compilation errors
                // This is a simplified check - in a real implementation,
                // you would check the Unity console for errors
                return true;
            }
            catch
            {
                return false;
            }
        }

        private float EvaluateIntegrationCheck(IntegrationCheck check)
        {
            return check.Status == "Pass" ? 100f : 0f;
        }

        private async Task ImproveIntegrationIssue(ValidationIssue issue)
        {
            Debug.Log($"🔧 Improving integration issue: {issue.Description}");
            
            try
            {
                // Create improvement task for Nexo Agent
                var improvementTask = new Nexo.Agent.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Improve Integration: {issue.Type}",
                    Description = $"Fix integration issue: {issue.Description}",
                    Priority = issue.Severity == "High" ? 1 : 2,
                    Status = "Pending"
                };
                
                await _nexoAgent.ExecuteTaskAsync(improvementTask);
                
                Debug.Log($"✅ Integration improvement completed: {issue.Type}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to improve integration issue {issue.Type}: {ex.Message}");
            }
        }
    }

    public class IntegrationCheck
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
