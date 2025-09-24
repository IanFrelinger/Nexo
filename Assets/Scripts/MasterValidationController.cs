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
    /// Master controller that orchestrates the entire validation and improvement process
    /// </summary>
    public class MasterValidationController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private NexoSelfValidator selfValidator;
        [SerializeField] private QualityImprovementEngine improvementEngine;
        [SerializeField] private NexoTaskOrchestrator taskOrchestrator;
        [SerializeField] private NexoDebugger debugger;
        
        [Header("Master Control UI")]
        [SerializeField] private Button startFullValidationButton;
        [SerializeField] private Button stopValidationButton;
        [SerializeField] private TextMeshProUGUI masterStatusText;
        [SerializeField] private Slider masterProgressBar;
        [SerializeField] private TextMeshProUGUI overallQualityText;
        [SerializeField] private TextMeshProUGUI iterationCountText;
        
        [Header("Validation Results")]
        [SerializeField] private TextMeshProUGUI validationSummaryText;
        [SerializeField] private ScrollRect validationSummaryScroll;
        
        [Header("Configuration")]
        [SerializeField] private bool enableContinuousValidation = false;
        [SerializeField] private float validationInterval = 60f;
        [SerializeField] private int maxTotalIterations = 5;
        
        private bool _isRunning = false;
        private int _totalIterations = 0;
        private List<ValidationCycle> _validationCycles = new List<ValidationCycle>();
        private float _overallQualityScore = 0f;
        
        private void Start()
        {
            InitializeMasterController();
            SetupUI();
        }
        
        private void InitializeMasterController()
        {
            Debug.Log("🎯 Initializing Master Validation Controller...");
            
            // Ensure all components are available
            if (selfValidator == null)
                selfValidator = FindObjectOfType<NexoSelfValidator>();
            
            if (improvementEngine == null)
                improvementEngine = FindObjectOfType<QualityImprovementEngine>();
            
            if (taskOrchestrator == null)
                taskOrchestrator = FindObjectOfType<NexoTaskOrchestrator>();
            
            if (debugger == null)
                debugger = FindObjectOfType<NexoDebugger>();
            
            UpdateMasterStatus("Master Validation Controller Ready");
            LogMaster("🎯 Master Controller initialized");
        }
        
        private void SetupUI()
        {
            if (startFullValidationButton != null)
                startFullValidationButton.onClick.AddListener(StartFullValidation);
            
            if (stopValidationButton != null)
                stopValidationButton.onClick.AddListener(StopValidation);
        }
        
        public async void StartFullValidation()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _totalIterations = 0;
            _validationCycles.Clear();
            
            LogMaster("🚀 Starting full validation and improvement cycle...");
            UpdateMasterStatus("Starting Full Validation Cycle");
            
            try
            {
                await ExecuteFullValidationCycle();
                CompleteFullValidation();
            }
            catch (Exception ex)
            {
                LogMaster($"❌ Full validation failed: {ex.Message}");
                FailFullValidation();
            }
            finally
            {
                _isRunning = false;
            }
        }
        
        public void StopValidation()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            LogMaster("⏹️ Validation stopped by user");
            UpdateMasterStatus("Validation Stopped");
        }
        
        private async Task ExecuteFullValidationCycle()
        {
            bool needsMoreIterations = true;
            
            while (needsMoreIterations && _totalIterations < maxTotalIterations && _isRunning)
            {
                _totalIterations++;
                LogMaster($"🔄 Starting validation cycle {_totalIterations}/{maxTotalIterations}");
                
                var cycle = new ValidationCycle
                {
                    CycleNumber = _totalIterations,
                    StartTime = DateTime.Now
                };
                
                try
                {
                    // Step 1: Generate/Regenerate components if needed
                    if (_totalIterations == 1)
                    {
                        await GenerateInitialComponents();
                    }
                    else
                    {
                        await RegenerateFailedComponents();
                    }
                    
                    // Step 2: Run validation
                    await RunValidationCycle(cycle);
                    
                    // Step 3: Check if improvement is needed
                    if (cycle.NeedsImprovement)
                    {
                        await RunImprovementCycle(cycle);
                        
                        // Step 4: Re-validate after improvement
                        await RunValidationCycle(cycle);
                    }
                    
                    // Step 5: Check if we need more iterations
                    needsMoreIterations = CheckIfMoreIterationsNeeded(cycle);
                    
                    cycle.EndTime = DateTime.Now;
                    cycle.Success = !needsMoreIterations;
                    
                    _validationCycles.Add(cycle);
                    
                    UpdateMasterProgress((float)_totalIterations / maxTotalIterations);
                    UpdateIterationCount(_totalIterations);
                }
                catch (Exception ex)
                {
                    LogMaster($"❌ Validation cycle {_totalIterations} failed: {ex.Message}");
                    cycle.EndTime = DateTime.Now;
                    cycle.Success = false;
                    cycle.Error = ex.Message;
                    _validationCycles.Add(cycle);
                    break;
                }
            }
        }
        
        private async Task GenerateInitialComponents()
        {
            LogMaster("🏗️ Generating initial components...");
            UpdateMasterStatus("Generating Initial Components");
            
            if (taskOrchestrator != null)
            {
                // Start the task orchestrator to generate initial components
                // This would trigger the full generation process
                LogMaster("📝 Starting component generation...");
                await Task.Delay(2000); // Simulate generation time
            }
            else
            {
                LogMaster("⚠️ Task orchestrator not available, skipping generation");
            }
        }
        
        private async Task RegenerateFailedComponents()
        {
            LogMaster("🔄 Regenerating failed components...");
            UpdateMasterStatus("Regenerating Failed Components");
            
            // Find components that failed validation in previous cycle
            var failedComponents = GetFailedComponentsFromLastCycle();
            
            if (failedComponents.Count > 0)
            {
                LogMaster($"🔧 Regenerating {failedComponents.Count} failed components");
                
                foreach (var component in failedComponents)
                {
                    await RegenerateComponent(component);
                }
            }
            else
            {
                LogMaster("✅ No components need regeneration");
            }
        }
        
        private async Task RunValidationCycle(ValidationCycle cycle)
        {
            LogMaster("🔍 Running validation cycle...");
            UpdateMasterStatus("Running Validation");
            
            if (selfValidator != null)
            {
                // Start the self validator
                selfValidator.StartValidation();
                
                // Wait for validation to complete
                while (selfValidator.IsValidating)
                {
                    await Task.Delay(1000);
                }
                
                // Get validation results
                var results = selfValidator.GetValidationResults();
                cycle.ValidationResults = new List<ValidationResult>(results);
                cycle.OverallScore = selfValidator.GetOverallScore;
                
                // Check if improvement is needed
                cycle.NeedsImprovement = results.Exists(r => !r.Passed);
                
                LogMaster($"📊 Validation complete: {cycle.OverallScore:P1} overall score");
            }
            else
            {
                LogMaster("⚠️ Self validator not available");
            }
        }
        
        private async Task RunImprovementCycle(ValidationCycle cycle)
        {
            LogMaster("🔧 Running improvement cycle...");
            UpdateMasterStatus("Running Improvement Cycle");
            
            if (improvementEngine != null)
            {
                var failedComponents = cycle.ValidationResults.FindAll(r => !r.Passed);
                
                foreach (var failedComponent in failedComponents)
                {
                    LogMaster($"🔧 Improving {failedComponent.Component}...");
                    
                    var improvementResult = await improvementEngine.ImproveComponent(failedComponent);
                    cycle.ImprovementResults.Add(improvementResult);
                    
                    if (improvementResult.Success)
                    {
                        LogMaster($"✅ {failedComponent.Component} improved: {improvementResult.ImprovementAmount:P1}");
                    }
                    else
                    {
                        LogMaster($"❌ {failedComponent.Component} improvement failed: {improvementResult.Error}");
                    }
                }
            }
            else
            {
                LogMaster("⚠️ Improvement engine not available");
            }
        }
        
        private bool CheckIfMoreIterationsNeeded(ValidationCycle cycle)
        {
            // Check if overall quality meets threshold
            if (cycle.OverallScore >= 0.8f)
            {
                LogMaster("✅ Quality threshold met, no more iterations needed");
                return false;
            }
            
            // Check if we have failed components
            var failedComponents = cycle.ValidationResults.FindAll(r => !r.Passed);
            if (failedComponents.Count == 0)
            {
                LogMaster("✅ No failed components, no more iterations needed");
                return false;
            }
            
            // Check if improvement was successful
            var successfulImprovements = cycle.ImprovementResults.FindAll(r => r.Success);
            if (successfulImprovements.Count == 0 && cycle.ImprovementResults.Count > 0)
            {
                LogMaster("⚠️ No successful improvements, stopping iterations");
                return false;
            }
            
            LogMaster($"🔧 {failedComponents.Count} components still need improvement");
            return true;
        }
        
        private List<string> GetFailedComponentsFromLastCycle()
        {
            if (_validationCycles.Count == 0) return new List<string>();
            
            var lastCycle = _validationCycles[_validationCycles.Count - 1];
            var failedComponents = new List<string>();
            
            foreach (var result in lastCycle.ValidationResults)
            {
                if (!result.Passed)
                {
                    failedComponents.Add(result.Component);
                }
            }
            
            return failedComponents;
        }
        
        private async Task RegenerateComponent(string componentName)
        {
            LogMaster($"🔄 Regenerating {componentName}...");
            
            if (taskOrchestrator != null)
            {
                // Use task orchestrator to regenerate the specific component
                // This would call the Nexo Agent to regenerate the component
                await Task.Delay(1000); // Simulate regeneration time
                LogMaster($"✅ {componentName} regenerated");
            }
        }
        
        private void CompleteFullValidation()
        {
            var totalDuration = DateTime.Now - _validationCycles[0].StartTime;
            var successfulCycles = _validationCycles.Count(c => c.Success);
            var totalImprovements = _validationCycles.Sum(c => c.ImprovementResults.Count);
            var successfulImprovements = _validationCycles.Sum(c => c.ImprovementResults.Count(r => r.Success));
            
            _overallQualityScore = _validationCycles.Count > 0 ? _validationCycles[_validationCycles.Count - 1].OverallScore : 0f;
            
            LogMaster("🎉 Full validation cycle completed!");
            LogMaster($"📊 Final Quality Score: {_overallQualityScore:P1}");
            LogMaster($"🔄 Total Iterations: {_totalIterations}");
            LogMaster($"✅ Successful Cycles: {successfulCycles}/{_validationCycles.Count}");
            LogMaster($"🔧 Total Improvements: {totalImprovements} ({successfulImprovements} successful)");
            LogMaster($"⏱️ Total Duration: {totalDuration.TotalMinutes:F1} minutes");
            
            UpdateMasterStatus("Full Validation Completed");
            UpdateMasterProgress(1f);
            UpdateOverallQuality(_overallQualityScore);
            DisplayValidationSummary();
        }
        
        private void FailFullValidation()
        {
            LogMaster("❌ Full validation cycle failed");
            UpdateMasterStatus("Full Validation Failed");
            UpdateMasterProgress(1f);
        }
        
        private void DisplayValidationSummary()
        {
            if (validationSummaryText != null)
            {
                var summary = "Validation Summary:\n\n";
                
                for (int i = 0; i < _validationCycles.Count; i++)
                {
                    var cycle = _validationCycles[i];
                    var status = cycle.Success ? "✅" : "❌";
                    var duration = (cycle.EndTime - cycle.StartTime).TotalSeconds;
                    
                    summary += $"Cycle {cycle.CycleNumber}: {status} {cycle.OverallScore:P1} ({duration:F1}s)\n";
                    
                    if (cycle.ImprovementResults.Count > 0)
                    {
                        summary += $"  Improvements: {cycle.ImprovementResults.Count(r => r.Success)}/{cycle.ImprovementResults.Count}\n";
                    }
                }
                
                summary += $"\nFinal Quality Score: {_overallQualityScore:P1}";
                summary += $"\nTotal Iterations: {_totalIterations}";
                
                validationSummaryText.text = summary;
            }
        }
        
        private void UpdateMasterStatus(string status)
        {
            if (masterStatusText != null)
                masterStatusText.text = status;
            
            if (debugger != null)
                debugger.LogInfo($"Master Controller: {status}");
        }
        
        private void UpdateMasterProgress(float progress)
        {
            if (masterProgressBar != null)
                masterProgressBar.value = progress;
        }
        
        private void UpdateOverallQuality(float score)
        {
            if (overallQualityText != null)
                overallQualityText.text = $"Overall Quality: {score:P1}";
        }
        
        private void UpdateIterationCount(int count)
        {
            if (iterationCountText != null)
                iterationCountText.text = $"Iterations: {count}/{maxTotalIterations}";
        }
        
        private void LogMaster(string message)
        {
            Debug.Log($"🎯 {message}");
            
            if (debugger != null)
                debugger.LogDebug(message);
        }
        
        // Public methods for external access
        public bool IsRunning => _isRunning;
        public float OverallQualityScore => _overallQualityScore;
        public int TotalIterations => _totalIterations;
        public List<ValidationCycle> GetValidationCycles => _validationCycles;
    }
    
    /// <summary>
    /// Validation cycle data
    /// </summary>
    [System.Serializable]
    public class ValidationCycle
    {
        public int CycleNumber;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool Success;
        public float OverallScore;
        public bool NeedsImprovement;
        public List<ValidationResult> ValidationResults = new List<ValidationResult>();
        public List<ImprovementResult> ImprovementResults = new List<ImprovementResult>();
        public string Error;
    }
}
