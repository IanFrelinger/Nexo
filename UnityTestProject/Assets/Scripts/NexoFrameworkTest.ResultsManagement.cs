using System;
using UnityEngine;
using TMPro;

namespace NexoDoomGame
{
    /// <summary>
    /// Test results handling and reporting functionality
    /// </summary>
    public partial class NexoFrameworkTest
    {
        private void CompleteTest()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"🎉 Test completed in {testDuration.TotalSeconds:F1} seconds");
            LogTest($"📊 Results: {_testResults.SuccessCount} successes, {_testResults.FailureCount} failures");
            
            UpdateTestStatus("Test Completed Successfully");
            UpdateTestProgress(1f);
            
            // Display results
            if (testResultsText != null)
            {
                testResultsText.text = $"Test Results:\n" +
                                     $"✅ Successes: {_testResults.SuccessCount}\n" +
                                     $"❌ Failures: {_testResults.FailureCount}\n" +
                                     $"⏱️ Duration: {testDuration.TotalSeconds:F1}s\n" +
                                     $"📊 Success Rate: {(_testResults.SuccessCount / (float)(_testResults.SuccessCount + _testResults.FailureCount) * 100):F1}%";
            }
        }
        
        private void FailTest()
        {
            var testDuration = DateTime.Now - _testStartTime;
            
            LogTest($"❌ Test failed after {testDuration.TotalSeconds:F1} seconds");
            LogTest($"📊 Results: {_testResults.SuccessCount} successes, {_testResults.FailureCount} failures");
            
            UpdateTestStatus("Test Failed");
            UpdateTestProgress(1f);
            
            // Display results
            if (testResultsText != null)
            {
                testResultsText.text = $"Test Results:\n" +
                                     $"✅ Successes: {_testResults.SuccessCount}\n" +
                                     $"❌ Failures: {_testResults.FailureCount}\n" +
                                     $"⏱️ Duration: {testDuration.TotalSeconds:F1}s\n" +
                                     $"📊 Success Rate: {(_testResults.SuccessCount / (float)(_testResults.SuccessCount + _testResults.FailureCount) * 100):F1}%";
            }
        }
    }
}
