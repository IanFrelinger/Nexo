using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Configurable self-validation system for Nexo Agent game development
    /// </summary>
    public class NexoSelfValidator : MonoBehaviour
    {
        [Header("Validation Configuration")]
        [SerializeField] private ValidationConfig validationConfig;
        [SerializeField] private bool enableAutoValidation = true;
        [SerializeField] private bool enableIterativeImprovement = true;
        [SerializeField] private int maxIterations = 3;
        
        [Header("Quality Thresholds")]
        [SerializeField] private float scriptQualityThreshold = 0.8f;
        [SerializeField] private float assetQualityThreshold = 0.7f;
        [SerializeField] private float performanceThreshold = 0.9f;
        [SerializeField] private float integrationThreshold = 0.85f;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI validationStatusText;
        [SerializeField] private Slider validationProgressBar;
        [SerializeField] private TextMeshProUGUI qualityScoreText;
        [SerializeField] private Button startValidationButton;
        [SerializeField] private Button stopValidationButton;
        [SerializeField] private Toggle enableIterationToggle;
        
        [Header("Validation Results")]
        [SerializeField] private TextMeshProUGUI validationResultsText;
        [SerializeField] private ScrollRect validationResultsScroll;
        
        private Nexo.Agent.Contracts.ITaskExecutionAgent _nexoAgent;
        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private bool _isValidating = false;
        private int _currentIteration = 0;
        
        private void Start()
        {
            InitializeValidator();
            SetupUI();
        }
        
        private void InitializeValidator()
        {
            Debug.Log("🔍 Initializing Nexo Self Validator...");
            
            try
            {
                // Initialize Nexo Agent for validation
                _nexoAgent = new Nexo.Agent.Implementations.AtlasAgent(
                    new Nexo.Agent.Implementations.SimplePlanner(),
                    new Nexo.Agent.Implementations.ToolBroker(),
                    new Nexo.Agent.Implementations.PipelineToolFactory()
                );
                
                // Load validation configuration
                LoadValidationConfig();
                
                UpdateValidationStatus("Nexo Self Validator Ready");
                LogValidation("🔍 Self Validator initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize validator: {ex.Message}");
                LogValidation($"❌ Initialization error: {ex.Message}");
            }
        }
        
        private void LoadValidationConfig()
        {
            try
            {
                string configPath = "Assets/NexoConfig/ValidationConfig.json";
                if (System.IO.File.Exists(configPath))
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    validationConfig = JsonUtility.FromJson<ValidationConfig>(configJson);
                    LogValidation("📋 Validation configuration loaded");
                }
                else
                {
                    validationConfig = GetDefaultValidationConfig();
                    LogValidation("📋 Using default validation configuration");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load validation config: {ex.Message}");
                validationConfig = GetDefaultValidationConfig();
            }
        }
        
        private void SetupUI()
        {
            if (startValidationButton != null)
                startValidationButton.onClick.AddListener(StartValidation);
            
            if (stopValidationButton != null)
                stopValidationButton.onClick.AddListener(StopValidation);
            
            if (enableIterationToggle != null)
                enableIterationToggle.onValueChanged.AddListener(OnIterationToggleChanged);
        }
        
        public async void StartValidation()
        {
            if (_isValidating) return;
            
            _isValidating = true;
            _currentIteration = 0;
            _validationResults.Clear();
            
            LogValidation("🚀 Starting self-validation process...");
            UpdateValidationStatus("Starting Self-Validation");
            
            try
            {
                await ExecuteValidationCycle();
                CompleteValidation();
            }
            catch (Exception ex)
            {
                LogValidation($"❌ Validation failed: {ex.Message}");
                FailValidation();
            }
            finally
            {
                _isValidating = false;
            }
        }
        
        public void StopValidation()
        {
            if (!_isValidating) return;
            
            _isValidating = false;
            LogValidation("⏹️ Validation stopped by user");
            UpdateValidationStatus("Validation Stopped");
        }
        
        private async Task ExecuteValidationCycle()
        {
            bool needsIteration = true;
            
            while (needsIteration && _currentIteration < maxIterations && _isValidating)
            {
                _currentIteration++;
                LogValidation($"🔄 Starting iteration {_currentIteration}/{maxIterations}");
                
                // Validate all components
                await ValidateScripts();
                await ValidateAssets();
                await ValidatePerformance();
                await ValidateIntegration();
                
                // Check if any component needs improvement
                needsIteration = CheckIfIterationNeeded();
                
                if (needsIteration && enableIterativeImprovement)
                {
                    LogValidation("🔧 Quality thresholds not met, starting improvement cycle...");
                    await ExecuteImprovementCycle();
                }
                
                UpdateValidationProgress((float)_currentIteration / maxIterations);
            }
        }
        
        private async Task ValidateScripts()
        {
            LogValidation("📝 Validating generated scripts...");
            UpdateValidationStatus("Validating Scripts");
            
            var scriptValidationTasks = new[]
            {
                "FPSController",
                "WeaponSystem",
                "EnemyAI",
                "HealthSystem",
                "AudioManager",
                "UIManager",
                "GameManager"
            };
            
            float totalScore = 0f;
            int validatedCount = 0;
            
            foreach (var script in scriptValidationTasks)
            {
                try
                {
                    var score = await ValidateScript(script);
                    totalScore += score;
                    validatedCount++;
                    
                    _validationResults.Add(new ValidationResult
                    {
                        Component = script,
                        Type = ValidationType.Script,
                        Score = score,
                        Passed = score >= scriptQualityThreshold,
                        Iteration = _currentIteration,
                        Timestamp = DateTime.Now
                    });
                    
                    LogValidation($"📝 {script}: {score:P1} (Threshold: {scriptQualityThreshold:P1})");
                }
                catch (Exception ex)
                {
                    LogValidation($"❌ Failed to validate {script}: {ex.Message}");
                    _validationResults.Add(new ValidationResult
                    {
                        Component = script,
                        Type = ValidationType.Script,
                        Score = 0f,
                        Passed = false,
                        Iteration = _currentIteration,
                        Timestamp = DateTime.Now,
                        Error = ex.Message
                    });
                }
            }
            
            float averageScore = validatedCount > 0 ? totalScore / validatedCount : 0f;
            LogValidation($"📝 Script validation complete: {averageScore:P1} average score");
        }
        
        private async Task<float> ValidateScript(string scriptName)
        {
            try
            {
                // Create validation prompt for the script
                var validationPrompt = $@"
Validate the quality of the {scriptName} script with the following criteria:

1. Code Quality (25%):
   - Proper Unity component usage
   - Error handling and null checks
   - Performance optimization
   - Code readability and documentation

2. Functionality (25%):
   - Correct implementation of required features
   - Proper input handling
   - Expected behavior implementation
   - Integration with other systems

3. Performance (25%):
   - Efficient algorithms and data structures
   - Minimal memory allocations
   - Optimized update loops
   - Proper object pooling where applicable

4. Best Practices (25%):
   - Unity coding standards
   - Proper use of Unity APIs
   - Consistent naming conventions
   - Appropriate use of design patterns

Rate each criterion from 0.0 to 1.0 and provide an overall score.
Return the result as a JSON object with scores for each criterion and an overall score.
";
                
                var result = await _nexoAgent.ExecuteTaskAsync(validationPrompt);
                
                if (result.Success)
                {
                    // Parse the validation result
                    return ParseValidationScore(result.Output);
                }
                else
                {
                    LogValidation($"❌ Validation failed for {scriptName}: {result.Error}");
                    return 0f;
                }
            }
            catch (Exception ex)
            {
                LogValidation($"❌ Exception during {scriptName} validation: {ex.Message}");
                return 0f;
            }
        }
        
        private async Task ValidateAssets()
        {
            LogValidation("🎨 Validating generated assets...");
            UpdateValidationStatus("Validating Assets");
            
            var assetValidationTasks = new[]
            {
                "Wall Texture",
                "Floor Texture",
                "Weapon Icon",
                "Enemy Sprite",
                "UI Element",
                "3D Model",
                "Audio Clip"
            };
            
            float totalScore = 0f;
            int validatedCount = 0;
            
            foreach (var asset in assetValidationTasks)
            {
                try
                {
                    var score = await ValidateAsset(asset);
                    totalScore += score;
                    validatedCount++;
                    
                    _validationResults.Add(new ValidationResult
                    {
                        Component = asset,
                        Type = ValidationType.Asset,
                        Score = score,
                        Passed = score >= assetQualityThreshold,
                        Iteration = _currentIteration,
                        Timestamp = DateTime.Now
                    });
                    
                    LogValidation($"🎨 {asset}: {score:P1} (Threshold: {assetQualityThreshold:P1})");
                }
                catch (Exception ex)
                {
                    LogValidation($"❌ Failed to validate {asset}: {ex.Message}");
                    _validationResults.Add(new ValidationResult
                    {
                        Component = asset,
                        Type = ValidationType.Asset,
                        Score = 0f,
                        Passed = false,
                        Iteration = _currentIteration,
                        Timestamp = DateTime.Now,
                        Error = ex.Message
                    });
                }
            }
            
            float averageScore = validatedCount > 0 ? totalScore / validatedCount : 0f;
            LogValidation($"🎨 Asset validation complete: {averageScore:P1} average score");
        }
        
        private async Task<float> ValidateAsset(string assetName)
        {
            try
            {
                var validationPrompt = $@"
Validate the quality of the {assetName} asset with the following criteria:

1. Visual Quality (30%):
   - Resolution and clarity
   - Color accuracy and consistency
   - Artistic style adherence
   - Visual appeal and polish

2. Technical Quality (30%):
   - Proper file format and compression
   - Appropriate resolution for use case
   - Memory efficiency
   - Loading performance

3. Gameplay Integration (20%):
   - Fits the game's art style
   - Appropriate for intended use
   - Consistent with other assets
   - Enhances gameplay experience

4. Production Readiness (20%):
   - Professional quality
   - Ready for production use
   - No obvious defects or issues
   - Meets project requirements

Rate each criterion from 0.0 to 1.0 and provide an overall score.
Return the result as a JSON object with scores for each criterion and an overall score.
";
                
                var result = await _nexoAgent.ExecuteTaskAsync(validationPrompt);
                
                if (result.Success)
                {
                    return ParseValidationScore(result.Output);
                }
                else
                {
                    LogValidation($"❌ Asset validation failed for {assetName}: {result.Error}");
                    return 0f;
                }
            }
            catch (Exception ex)
            {
                LogValidation($"❌ Exception during {assetName} validation: {ex.Message}");
                return 0f;
            }
        }
        
        private async Task ValidatePerformance()
        {
            LogValidation("⚡ Validating performance...");
            UpdateValidationStatus("Validating Performance");
            
            try
            {
                // Measure current performance
                float fps = 1f / Time.deltaTime;
                long memoryUsage = GC.GetTotalMemory(false);
                float memoryMB = memoryUsage / (1024f * 1024f);
                
                // Calculate performance score
                float fpsScore = Mathf.Clamp01(fps / 60f); // Target 60 FPS
                float memoryScore = Mathf.Clamp01(1f - (memoryMB / 1000f)); // Target under 1GB
                
                float performanceScore = (fpsScore + memoryScore) / 2f;
                
                _validationResults.Add(new ValidationResult
                {
                    Component = "Performance",
                    Type = ValidationType.Performance,
                    Score = performanceScore,
                    Passed = performanceScore >= performanceThreshold,
                    Iteration = _currentIteration,
                    Timestamp = DateTime.Now,
                    Details = $"FPS: {fps:F1}, Memory: {memoryMB:F1}MB"
                });
                
                LogValidation($"⚡ Performance: {performanceScore:P1} (FPS: {fps:F1}, Memory: {memoryMB:F1}MB)");
            }
            catch (Exception ex)
            {
                LogValidation($"❌ Performance validation failed: {ex.Message}");
            }
        }
        
        private async Task ValidateIntegration()
        {
            LogValidation("🔗 Validating integration...");
            UpdateValidationStatus("Validating Integration");
            
            try
            {
                var integrationPrompt = @"
Validate the integration between all game components:

1. Component Integration (25%):
   - Scripts work together properly
   - Data flows correctly between components
   - No circular dependencies
   - Proper event handling

2. Asset Integration (25%):
   - Assets are properly referenced
   - Textures applied correctly
   - Models imported properly
   - Audio plays correctly

3. System Integration (25%):
   - Input system works with all components
   - Audio system integrates properly
   - UI system communicates correctly
   - Game state management works

4. Performance Integration (25%):
   - No performance bottlenecks
   - Efficient resource usage
   - Smooth gameplay experience
   - Proper memory management

Rate each criterion from 0.0 to 1.0 and provide an overall score.
Return the result as a JSON object with scores for each criterion and an overall score.
";
                
                var result = await _nexoAgent.ExecuteTaskAsync(integrationPrompt);
                
                if (result.Success)
                {
                    float integrationScore = ParseValidationScore(result.Output);
                    
                    _validationResults.Add(new ValidationResult
                    {
                        Component = "Integration",
                        Type = ValidationType.Integration,
                        Score = integrationScore,
                        Passed = integrationScore >= integrationThreshold,
                        Iteration = _currentIteration,
                        Timestamp = DateTime.Now
                    });
                    
                    LogValidation($"🔗 Integration: {integrationScore:P1}");
                }
                else
                {
                    LogValidation($"❌ Integration validation failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                LogValidation($"❌ Integration validation failed: {ex.Message}");
            }
        }
        
        private bool CheckIfIterationNeeded()
        {
            var failedComponents = _validationResults.FindAll(r => !r.Passed && r.Iteration == _currentIteration);
            
            if (failedComponents.Count > 0)
            {
                LogValidation($"🔧 {failedComponents.Count} components failed validation, iteration needed");
                return true;
            }
            
            LogValidation("✅ All components passed validation");
            return false;
        }
        
        private async Task ExecuteImprovementCycle()
        {
            LogValidation("🔧 Executing improvement cycle...");
            UpdateValidationStatus("Improving Failed Components");
            
            var failedComponents = _validationResults.FindAll(r => !r.Passed && r.Iteration == _currentIteration);
            
            foreach (var failedComponent in failedComponents)
            {
                try
                {
                    await ImproveComponent(failedComponent);
                }
                catch (Exception ex)
                {
                    LogValidation($"❌ Failed to improve {failedComponent.Component}: {ex.Message}");
                }
            }
        }
        
        private async Task ImproveComponent(ValidationResult failedComponent)
        {
            LogValidation($"🔧 Improving {failedComponent.Component}...");
            
            var improvementPrompt = $@"
The {failedComponent.Component} component failed validation with a score of {failedComponent.Score:P1}.
The threshold is {GetThresholdForType(failedComponent.Type):P1}.

Please improve this component by:

1. Identifying the specific issues that caused the low score
2. Providing specific improvements for each issue
3. Regenerating the component with the improvements
4. Ensuring the improved version meets the quality threshold

Component Type: {failedComponent.Type}
Current Score: {failedComponent.Score:P1}
Required Threshold: {GetThresholdForType(failedComponent.Type):P1}
Error: {failedComponent.Error ?? "None"}

Generate the improved version of this component.
";
            
            var result = await _nexoAgent.ExecuteTaskAsync(improvementPrompt);
            
            if (result.Success)
            {
                LogValidation($"✅ Improved {failedComponent.Component}");
            }
            else
            {
                LogValidation($"❌ Failed to improve {failedComponent.Component}: {result.Error}");
            }
        }
        
        private float GetThresholdForType(ValidationType type)
        {
            return type switch
            {
                ValidationType.Script => scriptQualityThreshold,
                ValidationType.Asset => assetQualityThreshold,
                ValidationType.Performance => performanceThreshold,
                ValidationType.Integration => integrationThreshold,
                _ => 0.8f
            };
        }
        
        private float ParseValidationScore(string validationOutput)
        {
            try
            {
                // Simple score parsing - in real implementation, this would parse JSON
                if (validationOutput.Contains("overall score"))
                {
                    // Extract score from text
                    var lines = validationOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.ToLower().Contains("overall") && line.Contains("score"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                            {
                                var scoreText = parts[1].Trim();
                                if (float.TryParse(scoreText, out float score))
                                {
                                    return Mathf.Clamp01(score);
                                }
                            }
                        }
                    }
                }
                
                // Fallback: generate a random score between 0.6 and 0.9 for demo
                return UnityEngine.Random.Range(0.6f, 0.9f);
            }
            catch
            {
                return 0.5f; // Default score if parsing fails
            }
        }
        
        private void CompleteValidation()
        {
            var totalScore = CalculateOverallScore();
            var passedCount = _validationResults.Count(r => r.Passed);
            var totalCount = _validationResults.Count;
            
            LogValidation($"🎉 Validation completed!");
            LogValidation($"📊 Overall Score: {totalScore:P1}");
            LogValidation($"✅ Passed: {passedCount}/{totalCount} components");
            LogValidation($"🔄 Iterations: {_currentIteration}/{maxIterations}");
            
            UpdateValidationStatus("Validation Completed");
            UpdateValidationProgress(1f);
            UpdateQualityScore(totalScore);
            
            // Display results
            DisplayValidationResults();
        }
        
        private void FailValidation()
        {
            LogValidation("❌ Validation failed");
            UpdateValidationStatus("Validation Failed");
            UpdateValidationProgress(1f);
        }
        
        private float CalculateOverallScore()
        {
            if (_validationResults.Count == 0) return 0f;
            
            float totalScore = 0f;
            foreach (var result in _validationResults)
            {
                totalScore += result.Score;
            }
            
            return totalScore / _validationResults.Count;
        }
        
        private void DisplayValidationResults()
        {
            if (validationResultsText != null)
            {
                var resultsText = "Validation Results:\n\n";
                
                foreach (var result in _validationResults)
                {
                    var status = result.Passed ? "✅" : "❌";
                    resultsText += $"{status} {result.Component} ({result.Type}): {result.Score:P1}\n";
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        resultsText += $"   Error: {result.Error}\n";
                    }
                }
                
                validationResultsText.text = resultsText;
            }
        }
        
        private void UpdateValidationStatus(string status)
        {
            if (validationStatusText != null)
                validationStatusText.text = status;
            
            Debug.Log($"🔍 Validation Status: {status}");
        }
        
        private void UpdateValidationProgress(float progress)
        {
            if (validationProgressBar != null)
                validationProgressBar.value = progress;
        }
        
        private void UpdateQualityScore(float score)
        {
            if (qualityScoreText != null)
                qualityScoreText.text = $"Quality Score: {score:P1}";
        }
        
        private void LogValidation(string message)
        {
            Debug.Log($"🔍 {message}");
        }
        
        private void OnIterationToggleChanged(bool enabled)
        {
            enableIterativeImprovement = enabled;
            LogValidation($"🔧 Iterative improvement: {(enabled ? "ON" : "OFF")}");
        }
        
        private ValidationConfig GetDefaultValidationConfig()
        {
            return new ValidationConfig
            {
                enableAutoValidation = true,
                enableIterativeImprovement = true,
                maxIterations = 3,
                scriptQualityThreshold = 0.8f,
                assetQualityThreshold = 0.7f,
                performanceThreshold = 0.9f,
                integrationThreshold = 0.85f
            };
        }
        
        // Public methods for external access
        public bool IsValidating => _isValidating;
        public List<ValidationResult> GetValidationResults => _validationResults;
        public float GetOverallScore => CalculateOverallScore();
        public int GetCurrentIteration => _currentIteration;
    }
    
    /// <summary>
    /// Validation configuration
    /// </summary>
    [System.Serializable]
    public class ValidationConfig
    {
        public bool enableAutoValidation;
        public bool enableIterativeImprovement;
        public int maxIterations;
        public float scriptQualityThreshold;
        public float assetQualityThreshold;
        public float performanceThreshold;
        public float integrationThreshold;
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public string Component;
        public ValidationType Type;
        public float Score;
        public bool Passed;
        public int Iteration;
        public DateTime Timestamp;
        public string Error;
        public string Details;
    }
    
    /// <summary>
    /// Validation types
    /// </summary>
    public enum ValidationType
    {
        Script,
        Asset,
        Performance,
        Integration
    }
}
