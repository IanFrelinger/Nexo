using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Debug logging and console management functionality
    /// </summary>
    public partial class NexoTaskOrchestrator
    {
        private void LogDebug(string message)
        {
            if (debugConsole != null)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                debugConsole.text += $"[{timestamp}] {message}\n";
                
                // Auto-scroll to bottom
                if (debugScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    debugScrollRect.verticalNormalizedPosition = 0f;
                }
            }
            
            Debug.Log($"🔧 Debug: {message}");
        }
    }
}
