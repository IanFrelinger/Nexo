using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// UI functionality
    /// </summary>
    public partial class NexoCompositionSystem
    {
        private void UpdateCompositionStatus(string status)
        {
            if (compositionStatusText != null)
                compositionStatusText.text = status;
            
            Debug.Log($"🎯 Composition Status: {status}");
        }

        private void UpdateCompositionProgress(float progress)
        {
            if (compositionProgressBar != null)
                compositionProgressBar.value = progress;
        }
    }
}
