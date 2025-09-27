using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Business logic and decision making functionality
    /// </summary>
    public partial class MasterValidationController
    {
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
    }
}
