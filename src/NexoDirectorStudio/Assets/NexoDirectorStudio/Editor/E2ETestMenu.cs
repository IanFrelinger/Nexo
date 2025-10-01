using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using NexoDirectorStudio.Tests.EditMode;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Unity Editor menu items for running E2E smoke tests.
    /// Provides easy access to comprehensive testing from the Unity menu.
    /// </summary>
    public static class E2ETestMenu
    {
        [MenuItem("Nexo/Director Studio/Run E2E Smoke Tests")]
        public static async void RunE2ESmokeTests()
        {
            Debug.Log("🧪 Starting E2E smoke tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("E2E", "Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ E2E smoke tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ E2E smoke tests FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ E2E smoke tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Integration Tests")]
        public static async void RunIntegrationTests()
        {
            Debug.Log("🔗 Starting integration tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("Integration", "Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Integration tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ Integration tests FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Integration tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Performance Tests")]
        public static async void RunPerformanceTests()
        {
            Debug.Log("⚡ Starting performance tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("Performance", "Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Performance tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ Performance tests FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Performance tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run CI/CD Tests")]
        public static async void RunCICDTests()
        {
            Debug.Log("🚀 Starting CI/CD tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("CICD", "CI");
                
                if (result.Success)
                {
                    Debug.Log($"✅ CI/CD tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ CI/CD tests FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ CI/CD tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run All Tests")]
        public static async void RunAllTests()
        {
            Debug.Log("🎯 Starting comprehensive test suite...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunAllTestsAsync("Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ All tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ Some tests FAILED - {result.TestsFailed} failures");
                }

                // Export test report
                var reportPath = $"Assets/Generated/TestReports/comprehensive_test_{System.DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
                await TestRunnerConfiguration.ExportTestReportAsync(result, reportPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test suite failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Production Tests")]
        public static async void RunProductionTests()
        {
            Debug.Log("🏭 Starting production test suite...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunAllTestsAsync("Production");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Production tests PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                }
                else
                {
                    Debug.LogError($"❌ Production tests FAILED - {result.TestsFailed} failures");
                }

                // Export production test report
                var reportPath = $"Assets/Generated/TestReports/production_test_{System.DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
                await TestRunnerConfiguration.ExportTestReportAsync(result, reportPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Production test suite failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        [MenuItem("Nexo/Director Studio/Export Test Report")]
        public static async void ExportTestReport()
        {
            Debug.Log("📄 Generating comprehensive test report...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunAllTestsAsync("Development");
                var reportPath = $"Assets/Generated/TestReports/manual_test_{System.DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
                await TestRunnerConfiguration.ExportTestReportAsync(result, reportPath);
                
                Debug.Log($"📄 Test report exported to: {reportPath}");
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to export test report: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Quick Smoke Test")]
        public static async void QuickSmokeTest()
        {
            Debug.Log("⚡ Running quick smoke test...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("CICD", "CI");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Quick smoke test PASSED - {result.Duration.TotalSeconds:F1}s");
                }
                else
                {
                    Debug.LogError($"❌ Quick smoke test FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Quick smoke test failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Performance Benchmark")]
        public static async void PerformanceBenchmark()
        {
            Debug.Log("📊 Running performance benchmark...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("Performance", "Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Performance benchmark PASSED - {result.Duration.TotalSeconds:F1}s");
                    
                    if (result.PerformanceMetrics.ContainsKey("DurationMs"))
                    {
                        var durationMs = (long)result.PerformanceMetrics["DurationMs"];
                        Debug.Log($"📊 Pipeline Duration: {durationMs}ms");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Performance benchmark FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Performance benchmark failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Component Validation Test")]
        public static async void ComponentValidationTest()
        {
            Debug.Log("🧪 Running component validation test...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunTestCategoryAsync("ComponentValidation", "Development");
                
                if (result.Success)
                {
                    Debug.Log($"✅ Component validation PASSED - {result.TestsPassed}/{result.TestsRun} tests");
                    
                    if (result.PerformanceMetrics.ContainsKey("ValidationScore"))
                    {
                        var score = (float)result.PerformanceMetrics["ValidationScore"];
                        Debug.Log($"📊 Validation Score: {score:F1}%");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Component validation FAILED - {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Component validation test failed: {ex.Message}");
            }
        }
    }
}
