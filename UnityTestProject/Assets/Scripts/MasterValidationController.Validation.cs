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
    /// Validation cycle execution functionality
    /// </summary>
    public partial class MasterValidationController
    {
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
    }
}
