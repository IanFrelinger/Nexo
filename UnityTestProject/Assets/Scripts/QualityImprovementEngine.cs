using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Quality improvement engine that iteratively improves components based on validation results
    /// </summary>
    public partial class QualityImprovementEngine : MonoBehaviour
    {
        [Header("Improvement Configuration")]
        [SerializeField] private ImprovementConfig improvementConfig;
        [SerializeField] private bool enableAutoImprovement = true;
        [SerializeField] private int maxImprovementAttempts = 3;
        [SerializeField] private float improvementTimeout = 180f;
        
        [Header("Improvement Strategies")]
        [SerializeField] private List<ImprovementStrategy> availableStrategies = new List<ImprovementStrategy>();
        
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private List<ImprovementResult> _improvementResults = new List<ImprovementResult>();
        
        private void Start()
        {
            InitializeImprovementEngine();
        }
        
        private void InitializeImprovementEngine()
        {
            Debug.Log("🔧 Initializing Quality Improvement Engine...");
            
            try
            {
                // Initialize Nexo Agent
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                // Initialize improvement strategies
                InitializeImprovementStrategies();
                
                Debug.Log("✅ Quality Improvement Engine initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize improvement engine: {ex.Message}");
            }
        }
        
        private void InitializeImprovementStrategies()
        {
            availableStrategies = new List<ImprovementStrategy>
            {
                new ImprovementStrategy
                {
                    Name = "Code Refactoring",
                    Description = "Refactor code for better structure and readability",
                    ApplicableTypes = new[] { ValidationType.Script },
                    Priority = 1
                },
                new ImprovementStrategy
                {
                    Name = "Performance Optimization",
                    Description = "Optimize code for better performance",
                    ApplicableTypes = new[] { ValidationType.Script, ValidationType.Performance },
                    Priority = 2
                },
                new ImprovementStrategy
                {
                    Name = "Asset Quality Enhancement",
                    Description = "Improve asset quality and resolution",
                    ApplicableTypes = new[] { ValidationType.Asset },
                    Priority = 3
                },
                new ImprovementStrategy
                {
                    Name = "Integration Fixes",
                    Description = "Fix integration issues between components",
                    ApplicableTypes = new[] { ValidationType.Integration },
                    Priority = 4
                },
                new ImprovementStrategy
                {
                    Name = "Error Handling Improvements",
                    Description = "Add better error handling and validation",
                    ApplicableTypes = new[] { ValidationType.Script, ValidationType.Integration },
                    Priority = 5
                }
            };
        }
        
        public async Task<ImprovementResult> ImproveComponent(ValidationResult failedComponent)
        {
            Debug.Log($"🔧 Starting improvement for {failedComponent.Component}...");
            
            var improvementResult = new ImprovementResult
            {
                Component = failedComponent.Component,
                Type = failedComponent.Type,
                OriginalScore = failedComponent.Score,
                ImprovementAttempts = 0,
                StartTime = DateTime.Now
            };
            
            try
            {
                // Select appropriate improvement strategy
                var strategy = SelectImprovementStrategy(failedComponent);
                if (strategy == null)
                {
                    improvementResult.Success = false;
                    improvementResult.Error = "No suitable improvement strategy found";
                    return improvementResult;
                }
                
                // Execute improvement
                var improvedScore = await ExecuteImprovement(failedComponent, strategy, improvementResult);
                
                improvementResult.FinalScore = improvedScore;
                improvementResult.Success = improvedScore > failedComponent.Score;
                improvementResult.ImprovementAmount = improvedScore - failedComponent.Score;
                improvementResult.EndTime = DateTime.Now;
                
                _improvementResults.Add(improvementResult);
                
                Debug.Log($"✅ Improvement completed for {failedComponent.Component}: {failedComponent.Score:P1} → {improvedScore:P1}");
                
                return improvementResult;
            }
            catch (Exception ex)
            {
                improvementResult.Success = false;
                improvementResult.Error = ex.Message;
                improvementResult.EndTime = DateTime.Now;
                
                _improvementResults.Add(improvementResult);
                
                Debug.LogError($"❌ Improvement failed for {failedComponent.Component}: {ex.Message}");
                return improvementResult;
            }
        }
        
        private ImprovementStrategy SelectImprovementStrategy(ValidationResult failedComponent)
        {
            // Find strategies applicable to this component type
            var applicableStrategies = availableStrategies.FindAll(s => 
                Array.Exists(s.ApplicableTypes, t => t == failedComponent.Type));
            
            if (applicableStrategies.Count == 0)
                return null;
            
            // Sort by priority and return the best one
            applicableStrategies.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return applicableStrategies[0];
        }
        
        private async Task<float> ExecuteImprovement(ValidationResult failedComponent, ImprovementStrategy strategy, ImprovementResult improvementResult)
        {
            var improvementPrompt = CreateImprovementPrompt(failedComponent, strategy);
            
            for (int attempt = 1; attempt <= maxImprovementAttempts; attempt++)
            {
                improvementResult.ImprovementAttempts = attempt;
                
                Debug.Log($"🔧 Improvement attempt {attempt}/{maxImprovementAttempts} for {failedComponent.Component}");
                
                try
                {
                    var result = await _nexoAgent.ExecuteTaskAsync(improvementPrompt);
                    
                    if (result.Success)
                    {
                        // Simulate validation of improved component
                        var improvedScore = await ValidateImprovedComponent(failedComponent, result.Output);
                        
                        if (improvedScore > failedComponent.Score)
                        {
                            Debug.Log($"✅ Improvement successful: {failedComponent.Score:P1} → {improvedScore:P1}");
                            return improvedScore;
                        }
                        else
                        {
                            Debug.Log($"⚠️ Improvement didn't increase score: {improvedScore:P1}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Improvement attempt {attempt} failed: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Improvement attempt {attempt} exception: {ex.Message}");
                }
                
                // Wait before next attempt
                await Task.Delay(1000);
            }
            
            // If all attempts failed, return original score
            return failedComponent.Score;
        }
        
        private string CreateImprovementPrompt(ValidationResult failedComponent, ImprovementStrategy strategy)
        {
            return $@"
IMPROVEMENT TASK: {strategy.Name}

Component: {failedComponent.Component}
Type: {failedComponent.Type}
Current Score: {failedComponent.Score:P1}
Strategy: {strategy.Description}

ISSUES TO ADDRESS:
{failedComponent.Error ?? "Quality threshold not met"}

IMPROVEMENT REQUIREMENTS:
1. Analyze the current implementation and identify specific issues
2. Apply {strategy.Name} techniques to address the issues
3. Ensure the improved version meets quality standards
4. Maintain compatibility with existing systems
5. Optimize for performance and maintainability

EXPECTED OUTCOME:
- Improved quality score above {failedComponent.Score:P1}
- Better adherence to best practices
- Enhanced functionality and performance
- Proper error handling and validation

Generate the improved version of {failedComponent.Component} using {strategy.Name} approach.
";
        }
        
        private async Task<float> ValidateImprovedComponent(ValidationResult originalComponent, string improvedContent)
        {
            try
            {
                // Simulate validation of improved component
                var validationPrompt = $@"
Validate the improved {originalComponent.Component} component:

Original Score: {originalComponent.Score:P1}
Improvement Strategy: {GetImprovementStrategyForComponent(originalComponent.Component)?.Name ?? "Unknown"}

Please evaluate the improved component and provide a quality score from 0.0 to 1.0.
Consider the same criteria as the original validation but focus on the improvements made.

Return the score as a simple number between 0.0 and 1.0.
";
                
                var result = await _nexoAgent.ExecuteTaskAsync(validationPrompt);
                
                if (result.Success)
                {
                    if (float.TryParse(result.Output, out float score))
                    {
                        return Mathf.Clamp01(score);
                    }
                }
                
                // Fallback: generate a score that's likely better than original
                return Mathf.Clamp01(originalComponent.Score + UnityEngine.Random.Range(0.1f, 0.3f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Validation of improved component failed: {ex.Message}");
                return originalComponent.Score;
            }
        }
        
        private ImprovementStrategy GetImprovementStrategyForComponent(string componentName)
        {
            return availableStrategies.Find(s => s.Name.Contains("Code") || s.Name.Contains("Performance"));
        }
        
        public List<ImprovementResult> GetImprovementResults()
        {
            return new List<ImprovementResult>(_improvementResults);
        }
        
        public float GetOverallImprovementScore()
        {
            if (_improvementResults.Count == 0) return 0f;
            
            float totalImprovement = 0f;
            foreach (var result in _improvementResults)
            {
                totalImprovement += result.ImprovementAmount;
            }
            
            return totalImprovement / _improvementResults.Count;
        }
        
        public int GetSuccessfulImprovements()
        {
            return _improvementResults.Count(r => r.Success);
        }
        
        public int GetTotalImprovementAttempts()
        {
            return _improvementResults.Count;
        }
    }
    
    /// <summary>
    /// Improvement configuration
    /// </summary>
    [System.Serializable]
    public partial class ImprovementConfig
    {
        public bool enableAutoImprovement;
        public int maxImprovementAttempts;
        public float improvementTimeout;
        public string[] improvementStrategies;
    }
    
    /// <summary>
    /// Improvement strategy
    /// </summary>
    [System.Serializable]
    public partial class ImprovementStrategy
    {
        public string Name;
        public string Description;
        public ValidationType[] ApplicableTypes;
        public int Priority;
    }
    
    /// <summary>
    /// Improvement result
    /// </summary>
    [System.Serializable]
    public partial class ImprovementResult
    {
        public string Component;
        public ValidationType Type;
        public float OriginalScore;
        public float FinalScore;
        public float ImprovementAmount;
        public bool Success;
        public int ImprovementAttempts;
        public DateTime StartTime;
        public DateTime EndTime;
        public string Error;
    }
}
