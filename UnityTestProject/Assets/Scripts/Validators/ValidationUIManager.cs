using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame.Validators
{
    public partial class ValidationUIManager
    {
        private readonly TextMeshProUGUI _statusText;
        private readonly Slider _progressBar;
        private readonly TextMeshProUGUI _qualityScoreText;
        private readonly TextMeshProUGUI _resultsText;
        private readonly ScrollRect _resultsScroll;

        public ValidationUIManager(
            TextMeshProUGUI statusText,
            Slider progressBar,
            TextMeshProUGUI qualityScoreText,
            TextMeshProUGUI resultsText,
            ScrollRect resultsScroll)
        {
            _statusText = statusText;
            _progressBar = progressBar;
            _qualityScoreText = qualityScoreText;
            _resultsText = resultsText;
            _resultsScroll = resultsScroll;
        }

        public async Task UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
            
            Debug.Log($"📊 Status: {status}");
            await Task.Delay(10); // Small delay for UI update
        }

        public async Task UpdateProgress(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Clamp01(progress);
            }
            
            Debug.Log($"📊 Progress: {progress:P0}");
            await Task.Delay(10); // Small delay for UI update
        }

        public async Task UpdateQualityScore(float score)
        {
            if (_qualityScoreText != null)
            {
                _qualityScoreText.text = $"Quality Score: {score:P0}";
                _qualityScoreText.color = GetScoreColor(score);
            }
            
            Debug.Log($"📊 Quality Score: {score:P0}");
            await Task.Delay(10); // Small delay for UI update
        }

        public async Task UpdateResults(List<ValidationResult> results)
        {
            if (_resultsText != null)
            {
                var resultsText = GenerateResultsText(results);
                _resultsText.text = resultsText;
            }
            
            if (_resultsScroll != null)
            {
                _resultsScroll.verticalNormalizedPosition = 1f; // Scroll to top
            }
            
            Debug.Log($"📊 Results updated: {results.Count} validation results");
            await Task.Delay(10); // Small delay for UI update
        }

        private string GenerateResultsText(List<ValidationResult> results)
        {
            var text = "=== VALIDATION RESULTS ===\n\n";
            
            foreach (var result in results)
            {
                text += $"Phase: {result.Phase}\n";
                text += $"Score: {result.Score:P0}\n";
                text += $"Threshold: {result.Threshold:P0}\n";
                text += $"Status: {(result.Passed ? "✅ PASSED" : "❌ FAILED")}\n";
                text += $"Issues: {result.Issues.Count}\n";
                text += $"Timestamp: {result.Timestamp:HH:mm:ss}\n";
                
                if (result.Issues.Count > 0)
                {
                    text += "\nIssues:\n";
                    foreach (var issue in result.Issues)
                    {
                        text += $"  • {issue.Type}: {issue.Description} ({issue.Severity})\n";
                    }
                }
                
                text += "\n" + new string('-', 40) + "\n\n";
            }
            
            return text;
        }

        private Color GetScoreColor(float score)
        {
            if (score >= 0.8f) return Color.green;
            if (score >= 0.6f) return Color.yellow;
            return Color.red;
        }
    }
}
