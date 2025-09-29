using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Tests.Integration.Configuration;

namespace Nexo.Tests.Integration.Utilities
{
    /// <summary>
    /// Helper utilities for integration testing
    /// </summary>
    public static class IntegrationTestHelper
    {
        /// <summary>
        /// Creates a unique test directory for integration tests
        /// </summary>
        public static string CreateTestDirectory(string basePath = null)
        {
            var testPath = basePath ?? Path.Combine(Path.GetTempPath(), $"nexo-test-{Guid.NewGuid():N}");
            
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            
            return testPath;
        }

        /// <summary>
        /// Cleans up test directories and files
        /// </summary>
        public static void CleanupTestDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to cleanup test directory {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Measures execution time of an async operation
        /// </summary>
        public static async Task<(T Result, TimeSpan ExecutionTime)> MeasureExecutionTimeAsync<T>(Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// Measures execution time of a synchronous operation
        /// </summary>
        public static (T Result, TimeSpan ExecutionTime) MeasureExecutionTime<T>(Func<T> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// Gets current memory usage in MB
        /// </summary>
        public static long GetCurrentMemoryUsageMB()
        {
            var process = Process.GetCurrentProcess();
            return process.WorkingSet64 / 1024 / 1024;
        }

        /// <summary>
        /// Gets current handle count
        /// </summary>
        public static int GetCurrentHandleCount()
        {
            var process = Process.GetCurrentProcess();
            return process.HandleCount;
        }

        /// <summary>
        /// Waits for a condition to be true with timeout
        /// </summary>
        public static async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, TimeSpan checkInterval = default)
        {
            if (checkInterval == default)
                checkInterval = TimeSpan.FromMilliseconds(100);

            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (condition())
                    return true;
                    
                await Task.Delay(checkInterval);
            }
            
            return false;
        }

        /// <summary>
        /// Executes multiple operations concurrently with error handling
        /// </summary>
        public static async Task<List<T>> ExecuteConcurrentlyAsync<T>(
            IEnumerable<Func<Task<T>>> operations, 
            int maxConcurrency = 5)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = operations.Select(async operation =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await operation();
                }
                finally
                {
                    semaphore.Release();
                }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// Retries an operation with exponential backoff
        /// </summary>
        public static async Task<T> RetryAsync<T>(
            Func<Task<T>> operation, 
            int maxRetries = 3, 
            TimeSpan initialDelay = default,
            double backoffMultiplier = 2.0)
        {
            if (initialDelay == default)
                initialDelay = TimeSpan.FromMilliseconds(100);

            var delay = initialDelay;
            Exception lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt == maxRetries)
                        break;
                        
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * backoffMultiplier);
                }
            }

            throw new InvalidOperationException($"Operation failed after {maxRetries + 1} attempts", lastException);
        }

        /// <summary>
        /// Validates test configuration
        /// </summary>
        public static void ValidateTestConfiguration(IntegrationTestConfiguration config)
        {
            try
            {
                config.Validate();
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Invalid test configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a test file with specified content
        /// </summary>
        public static string CreateTestFile(string directory, string fileName, string content = null)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content ?? $"Test file content - {Guid.NewGuid()}");
            return filePath;
        }

        /// <summary>
        /// Creates a test assembly file (dummy)
        /// </summary>
        public static string CreateTestAssembly(string directory, string assemblyName = "TestAssembly.dll")
        {
            var assemblyPath = Path.Combine(directory, assemblyName);
            
            // Create a dummy assembly file
            var assemblyContent = $@"
// Dummy assembly for integration testing
// Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
// Test ID: {Guid.NewGuid()}

using System;

namespace TestAssembly
{{
    public class TestClass
    {{
        public string TestProperty {{ get; set; }} = ""TestValue"";
        
        public void TestMethod()
        {{
            Console.WriteLine(""Test method executed"");
        }}
    }}
}}";
            
            File.WriteAllText(assemblyPath, assemblyContent);
            return assemblyPath;
        }

        /// <summary>
        /// Generates a test report
        /// </summary>
        public static string GenerateTestReport(Dictionary<string, object> testResults, string outputPath = null)
        {
            var reportPath = outputPath ?? Path.Combine(Path.GetTempPath(), $"nexo-test-report-{Guid.NewGuid():N}.json");
            
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                TestResults = testResults,
                SystemInfo = new
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    Is64BitProcess = Environment.Is64BitProcess
                }
            };

            File.WriteAllText(reportPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return reportPath;
        }

        /// <summary>
        /// Checks if the system meets minimum requirements for integration tests
        /// </summary>
        public static bool CheckSystemRequirements()
        {
            try
            {
                // Check available memory (at least 1GB)
                var availableMemory = GC.GetTotalMemory(false);
                if (availableMemory < 1024 * 1024 * 1024) // 1GB
                    return false;

                // Check processor count (at least 2 cores)
                if (Environment.ProcessorCount < 2)
                    return false;

                // Check if we can create temporary files
                var tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath))
                    return false;

                var testFile = Path.Combine(tempPath, $"nexo-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
