using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Test discovery functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        /// <summary>
        /// Discovers all available tests.
        /// </summary>
        public async Task<IEnumerable<TestInfo>> DiscoverTestsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Discovering C# tests");

            _discoveredTests.Clear();

            try
            {
                // Get all assemblies in the current domain
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !a.FullName?.StartsWith("System.") == true)
                    .ToList();

                foreach (var assembly in assemblies)
                {
                    await DiscoverTestsInAssemblyAsync(assembly, cancellationToken);
                }

                _logger.LogInformation("Discovered {TestCount} C# tests", _discoveredTests.Count);
                return _discoveredTests.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover C# tests");
                return _discoveredTests.ToList();
            }
        }

        private async Task DiscoverTestsInAssemblyAsync(Assembly assembly, CancellationToken cancellationToken)
        {
            try
            {
                var testClasses = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                    .ToList();

                foreach (var testClass in testClasses)
                {
                    await DiscoverTestsInClassAsync(testClass, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover tests in assembly: {AssemblyName}", assembly.FullName);
            }
        }

        private Task DiscoverTestsInClassAsync(Type testClass, CancellationToken cancellationToken)
        {
            try
            {
                var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                    .ToList();

                foreach (var method in testMethods)
                {
                    var testInfo = CreateTestInfo(method, testClass);
                    if (testInfo != null)
                    {
                        _discoveredTests.Add(testInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover tests in class: {ClassName}", testClass.Name);
            }
            
            return Task.CompletedTask;
        }

        private TestInfo? CreateTestInfo(MethodInfo method, Type testClass)
        {
            try
            {
                var testAttribute = method.GetCustomAttribute<TestAttribute>();
                if (testAttribute == null)
                    return null;

                var testClassAttribute = testClass.GetCustomAttribute<TestClassAttribute>();
                var category = GetTestCategory(testAttribute);
                var priority = testAttribute.Priority;
                var estimatedDuration = TimeSpan.FromSeconds(testAttribute.EstimatedDurationSeconds);
                var timeout = TimeSpan.FromSeconds(testAttribute.TimeoutSeconds);
                var dependencies = testAttribute.Dependencies.ToList();
                var tags = testAttribute.Tags.ToList();
                var isEnabled = testAttribute.IsEnabled && (testClassAttribute?.IsEnabled ?? true);
                var description = !string.IsNullOrEmpty(testAttribute.Description) 
                    ? testAttribute.Description 
                    : testClassAttribute?.Description ?? string.Empty;

                var testId = $"{testClass.Name}.{method.Name}";
                var displayName = !string.IsNullOrEmpty(testAttribute.DisplayName) 
                    ? testAttribute.DisplayName 
                    : $"{testClass.Name}.{method.Name}";

                return new TestInfo(
                    testId,
                    displayName,
                    method,
                    testClass,
                    category,
                    priority,
                    estimatedDuration,
                    timeout,
                    dependencies,
                    tags,
                    isEnabled,
                    description
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create test info for method: {MethodName}", method.Name);
                return null;
            }
        }

        private TestCategory GetTestCategory(TestAttribute testAttribute)
        {
            return testAttribute switch
            {
                AiConnectivityTestAttribute => TestCategory.Integration,
                DomainAnalysisTestAttribute => TestCategory.Functional,
                CodeGenerationTestAttribute => TestCategory.Functional,
                EndToEndTestAttribute => TestCategory.E2E,
                PerformanceTestAttribute => TestCategory.Performance,
                ValidationTestAttribute => TestCategory.Unit,
                _ => TestCategory.Functional
            };
        }
    }
}
