using System;
using UnityEngine;
using UnityEngine.UI;

namespace NexoDoomGame
{
    /// <summary>
    /// Basic test setup and main orchestration functionality
    /// </summary>
    public partial class NexoFrameworkTest
    {
        private void InitializeTest()
        {
            Debug.Log("🧪 Initializing Nexo Framework Test...");
            
            // Set up UI
            if (startTestButton != null)
                startTestButton.onClick.AddListener(() => StartCoroutine(RunFullFrameworkTest()));
            
            if (stopTestButton != null)
                stopTestButton.onClick.AddListener(StopTest);
            
            // Initialize test results
            _testResults = new TestResults();
            
            UpdateTestStatus("Nexo Framework Test Ready");
            LogTest("🧪 Test system initialized");
        }
    }
}
