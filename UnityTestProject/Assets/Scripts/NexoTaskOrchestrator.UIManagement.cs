using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// UI updates and user interaction functionality
    /// </summary>
    public partial class NexoTaskOrchestrator
    {
        private void SetupUI()
        {
            if (startGenerationButton != null)
                startGenerationButton.onClick.AddListener(StartGeneration);
            
            if (stopGenerationButton != null)
                stopGenerationButton.onClick.AddListener(StopGeneration);
            
            if (enableDebugMode != null)
                enableDebugMode.onValueChanged.AddListener(OnDebugModeChanged);
        }
        
        private void OnDebugModeChanged(bool enabled)
        {
            if (debugConsole != null)
                debugConsole.gameObject.SetActive(enabled);
            
            if (debugScrollRect != null)
                debugScrollRect.gameObject.SetActive(enabled);
            
            LogDebug($"🔧 Debug mode: {(enabled ? "ON" : "OFF")}");
        }
        
        private void UpdateStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
            
            Debug.Log($"📊 Status: {status}");
        }
        
        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
                progressBar.value = progress;
        }
    }
}
