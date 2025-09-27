using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NexoDoomGame.Validators;

namespace NexoDoomGame
{
    /// <summary>
    /// Orchestrator for Nexo self-validation system that delegates to specialized validators.
    /// </summary>
    public partial class NexoSelfValidator : MonoBehaviour
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
        
        // Specialized validators
        private ScriptValidator _scriptValidator;
        private AssetValidator _assetValidator;
        private PerformanceValidator _performanceValidator;
        private IntegrationValidator _integrationValidator;
        private ValidationUIManager _uiManager;

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
                    new Nexo.Agent.Implementations.ExecutionEngine()
                );
                
                // Initialize specialized validators
                _scriptValidator = new ScriptValidator(_nexoAgent);
                _assetValidator = new AssetValidator(_nexoAgent);
                _performanceValidator = new PerformanceValidator(_nexoAgent);
                _integrationValidator = new IntegrationValidator(_nexoAgent);
                _uiManager = new ValidationUIManager(
                    validationStatusText,
                    validationProgressBar,
                    qualityScoreText,
                    validationResultsText,
                    validationResultsScroll
                );
                
                Debug.Log("✅ Nexo Self Validator initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Nexo Self Validator: {ex.Message}");
            }
        }

        private void SetupUI()
        {
            if (startValidationButton != null)
                startValidationButton.onClick.AddListener(StartValidation);
            
            if (stopValidationButton != null)
                stopValidationButton.onClick.AddListener(StopValidation);
            
            if (enableIterationToggle != null)
                enableIterationToggle.isOn = enableIterativeImprovement;
        }

        public async void StartValidation()
        {
            if (_isValidating) return;
            
            _isValidating = true;
            _currentIteration = 0;
            _validationResults.Clear();
            
            Debug.Log("🚀 Starting comprehensive validation...");
            
            try
            {
                await _uiManager.UpdateStatus("Starting validation...");
                await _uiManager.UpdateProgress(0f);
                
                // Run all validation phases
                await RunValidationPhases();
                
                await _uiManager.UpdateStatus("Validation completed!");
                await _uiManager.UpdateProgress(1f);
                
                Debug.Log("✅ Validation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Validation failed: {ex.Message}");
                await _uiManager.UpdateStatus($"Validation failed: {ex.Message}");
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
            Debug.Log("⏹️ Validation stopped by user");
        }

        private async Task RunValidationPhases()
        {
            var phases = new[]
            {
                ("Script Quality", () => _scriptValidator.ValidateScriptsAsync(scriptQualityThreshold)),
                ("Asset Quality", () => _assetValidator.ValidateAssetsAsync(assetQualityThreshold)),
                ("Performance", () => _performanceValidator.ValidatePerformanceAsync(performanceThreshold)),
                ("Integration", () => _integrationValidator.ValidateIntegrationAsync(integrationThreshold))
            };
            
            for (int i = 0; i < phases.Length; i++)
            {
                var (phaseName, validationTask) = phases[i];
                
                if (!_isValidating) break;
                
                await _uiManager.UpdateStatus($"Validating {phaseName}...");
                await _uiManager.UpdateProgress((float)i / phases.Length);
                
                try
                {
                    var result = await validationTask();
                    _validationResults.Add(result);
                    
                    if (result.Score < GetThresholdForPhase(phaseName))
                    {
                        Debug.LogWarning($"⚠️ {phaseName} validation failed with score: {result.Score:F2}");
                        
                        if (enableIterativeImprovement && _currentIteration < maxIterations)
                        {
                            await RunIterativeImprovement(phaseName, result);
                        }
                    }
                    else
                    {
                        Debug.Log($"✅ {phaseName} validation passed with score: {result.Score:F2}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ {phaseName} validation error: {ex.Message}");
                }
            }
        }

        private async Task RunIterativeImprovement(string phaseName, ValidationResult result)
        {
            _currentIteration++;
            Debug.Log($"🔄 Running iterative improvement for {phaseName} (iteration {_currentIteration})");
            
            await _uiManager.UpdateStatus($"Improving {phaseName}...");
            
            // Delegate to appropriate validator for improvement
            switch (phaseName)
            {
                case "Script Quality":
                    await _scriptValidator.ImproveScriptsAsync(result);
                    break;
                case "Asset Quality":
                    await _assetValidator.ImproveAssetsAsync(result);
                    break;
                case "Performance":
                    await _performanceValidator.ImprovePerformanceAsync(result);
                    break;
                case "Integration":
                    await _integrationValidator.ImproveIntegrationAsync(result);
                    break;
            }
        }

        private float GetThresholdForPhase(string phaseName)
        {
            return phaseName switch
            {
                "Script Quality" => scriptQualityThreshold,
                "Asset Quality" => assetQualityThreshold,
                "Performance" => performanceThreshold,
                "Integration" => integrationThreshold,
                _ => 0.8f
            };
        }
    }
}
