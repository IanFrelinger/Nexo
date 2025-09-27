using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Type analysis functionality for reflection-based coverage analyzer.
    /// </summary>
    public sealed partial class ReflectionBasedCoverageAnalyzer
    {
        private async Task<ClassCoverageInfo?> AnalyzeTypeAsync(
            Type type,
            List<Assembly> testAssemblies,
            CancellationToken cancellationToken)
        {
            try
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => !m.IsSpecialName && !m.IsAbstract)
                    .ToList();

                var totalMethods = methods.Count;
                var coveredMethods = 0;
                var uncoveredMethods = new List<string>();

                // Check if each method is covered by tests
                foreach (var method in methods)
                {
                    var isCovered = await IsMethodCoveredAsync(method, testAssemblies, cancellationToken);
                    if (isCovered)
                    {
                        coveredMethods++;
                    }
                    else
                    {
                        uncoveredMethods.Add(method.Name);
                    }
                }

                // Estimate line coverage based on method coverage
                var methodCoverage = totalMethods > 0 ? (double)coveredMethods / totalMethods * 100 : 0;
                var estimatedLinesPerMethod = 10; // Simplified estimation
                var totalLines = totalMethods * estimatedLinesPerMethod;
                var coveredLines = coveredMethods * estimatedLinesPerMethod;
                var lineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;

                return new ClassCoverageInfo(
                    type.Name,
                    type.Namespace ?? string.Empty,
                    lineCoverage,
                    methodCoverage,
                    totalLines,
                    coveredLines,
                    totalMethods,
                    coveredMethods,
                    uncoveredMethods
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze type: {TypeName}", type.Name);
                return null;
            }
        }

        private Task<bool> IsMethodCoveredAsync(
            MethodInfo method,
            List<Assembly> testAssemblies,
            CancellationToken cancellationToken)
        {
            try
            {
                // Simplified coverage detection - check if there are any test methods that might call this method
                foreach (var testAssembly in testAssemblies)
                {
                    var testTypes = testAssembly.GetTypes()
                        .Where(t => t.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var testType in testTypes)
                    {
                        var testMethods = testType.GetMethods()
                            .Where(m => m.Name.StartsWith("Test", StringComparison.OrdinalIgnoreCase) ||
                                       m.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var testMethod in testMethods)
                        {
                            // Check if test method name suggests it tests the target method
                            if (testMethod.Name.Contains(method.Name, StringComparison.OrdinalIgnoreCase) ||
                                testMethod.Name.Contains("Test" + method.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                return Task.FromResult(true);
                            }
                        }
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check method coverage: {MethodName}", method.Name);
                return Task.FromResult(false);
            }
        }
    }
}
