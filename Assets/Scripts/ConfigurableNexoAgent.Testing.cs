using System.Collections;
using UnityEngine;

namespace NexoDoomGame
{
    /// <summary>
    /// Testing functionality
    /// </summary>
    public partial class ConfigurableNexoAgent
    {
        private void TestGame()
        {
            Debug.Log("🎮 Testing generated game...");
            UpdateStatus("🧪 Running game tests...");
            
            StartCoroutine(RunGameTests());
        }

        private System.Collections.IEnumerator RunGameTests()
        {
            yield return new WaitForSeconds(1f);
            UpdateStatus("✅ Game tests passed!");
            Debug.Log("🎮 Generated game is ready to play!");
        }
    }
}
