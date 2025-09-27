using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Smoke test reporting functionality
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunSmokeTest(string testName, Func<Task> testAction)
        {
            _totalTests++;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await testAction();
                _passedTests++;
                stopwatch.Stop();

                var result = new SmokeTestResult(testName, true, stopwatch.Elapsed, null);
                _results.Add(result);

                Console.WriteLine($"   SUCCESS: {testName} - PASSED ({stopwatch.Elapsed.TotalMilliseconds:F0}ms)");
            }
            catch (Exception ex)
            {
                _failedTests++;
                stopwatch.Stop();

                var result = new SmokeTestResult(testName, false, stopwatch.Elapsed, ex.Message);
                _results.Add(result);

                Console.WriteLine($"   ERROR: {testName} - FAILED ({stopwatch.Elapsed.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"      Error: {ex.Message}");
            }
        }

        private void GenerateSmokeTestReport(TimeSpan totalDuration)
        {
            Console.WriteLine("Hot Smoke Test Report");
            Console.WriteLine("===================");
            Console.WriteLine($"Total Tests: {_totalTests}");
            Console.WriteLine($"Passed: {_passedTests} SUCCESS:");
            Console.WriteLine($"Failed: {_failedTests} ERROR:");
            Console.WriteLine($"Success Rate: {(_passedTests / (double)_totalTests * 100):F1}%");
            Console.WriteLine($"Total Duration: {totalDuration.TotalSeconds:F1}s");
            Console.WriteLine();

            if (_failedTests > 0)
            {
                Console.WriteLine("ERROR: Failed Tests:");
                foreach (var result in _results.Where(r => !r.Passed))
                {
                    Console.WriteLine($"   • {result.TestName}: {result.ErrorMessage}");
                }
                Console.WriteLine();
            }

            // Group by test suite
            var testSuites = new Dictionary<string, List<SmokeTestResult>>();
            foreach (var result in _results)
            {
                var suite = result.TestName.Split(':')[0];
                if (!testSuites.ContainsKey(suite))
                    testSuites[suite] = new List<SmokeTestResult>();
                testSuites[suite].Add(result);
            }

            Console.WriteLine("Stats Test Suite Summary:");
            foreach (var suite in testSuites)
            {
                var suitePassed = suite.Value.Count(r => r.Passed);
                var suiteTotal = suite.Value.Count;
                var suiteRate = (suitePassed / (double)suiteTotal * 100);

                Console.WriteLine($"   {suite.Key}: {suitePassed}/{suiteTotal} ({suiteRate:F1}%)");
            }
            Console.WriteLine();

            var overallSuccess = _failedTests == 0;
            Console.WriteLine($"Target Overall Result: {(overallSuccess ? "ALL SMOKE TESTS PASSED! Trophy" : "SOME SMOKE TESTS FAILED ERROR:")}");

            if (overallSuccess)
            {
                Console.WriteLine("SUCCESS: Test Aggregator is ready for production use!");
                Console.WriteLine("SUCCESS: All core functionality verified and working correctly!");
                Console.WriteLine("SUCCESS: Performance and reliability confirmed!");
            }
            else
            {
                Console.WriteLine("WARNING:  Issues detected - review failed tests before production use");
            }
        }
    }
}
