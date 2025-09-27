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
    /// UI management and display functionality
    /// </summary>
    public partial class MasterValidationController
    {
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
    }
}
