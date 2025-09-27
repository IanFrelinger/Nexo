using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// UI updates and user interaction functionality
    /// </summary>
    public partial class NexoFrameworkTest
    {
        private void UpdateTestStatus(string status)
        {
            if (testStatusText != null)
                testStatusText.text = status;
            
            if (debugger != null)
                debugger.LogInfo($"Test Status: {status}");
        }
        
        private void UpdateTestProgress(float progress)
        {
            if (testProgressBar != null)
                testProgressBar.value = progress;
        }
        
        private void LogTest(string message)
        {
            Debug.Log($"🧪 {message}");
            
            if (debugger != null)
                debugger.LogDebug(message);
        }
    }
}
