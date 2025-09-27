using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// UI functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
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
