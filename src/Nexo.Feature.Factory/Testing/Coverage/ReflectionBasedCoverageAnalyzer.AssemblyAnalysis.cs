using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Assembly analysis functionality for reflection-based coverage analyzer.
    /// </summary>
    public sealed partial class ReflectionBasedCoverageAnalyzer
    {
        /// <summary>
        /// Analyzes test coverage for the specified assemblies.
        /// </summary>
        public async Task<TestCoverageInfo> AnalyzeCoverageAsync(
            IEnumerable<string> assemblyPaths,
            IEnumerable<string> testAssemblyPaths,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting coverage analysis for {AssemblyCount} assemblies", assemblyPaths.Count());

            try
            {
                var sourceAssemblies = new List<Assembly>();
                var testAssemblies = new List<Assembly>();

                // Load source assemblies
                foreach (var assemblyPath in assemblyPaths)
                {
                    if (File.Exists(assemblyPath))
                    {
                        var assembly = Assembly.LoadFrom(assemblyPath);
                        sourceAssemblies.Add(assembly);
                        _logger.LogDebug("Loaded source assembly: {AssemblyName}", assembly.GetName().Name);
                    }
                }

                // Load test assemblies
                foreach (var testAssemblyPath in testAssemblyPaths)
                {
                    if (File.Exists(testAssemblyPath))
                    {
                        var assembly = Assembly.LoadFrom(testAssemblyPath);
                        testAssemblies.Add(assembly);
                        _logger.LogDebug("Loaded test assembly: {AssemblyName}", assembly.GetName().Name);
                    }
                }

                return await AnalyzeAssembliesAsync(sourceAssemblies, testAssemblies, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze coverage for assemblies");
                return CreateEmptyCoverageInfo();
            }
        }

        private async Task<TestCoverageInfo> AnalyzeAssembliesAsync(
            List<Assembly> sourceAssemblies,
            List<Assembly> testAssemblies,
            CancellationToken cancellationToken)
        {
            var fileCoverage = new Dictionary<string, FileCoverageInfo>();
            var classCoverage = new Dictionary<string, ClassCoverageInfo>();

            // Analyze each source assembly
            foreach (var assembly in sourceAssemblies)
            {
                var assemblyCoverage = await AnalyzeAssemblyAsync(assembly, testAssemblies, cancellationToken);
                
                foreach (var file in assemblyCoverage.FileCoverage)
                {
                    fileCoverage[file.Key] = file.Value;
                }
                
                foreach (var @class in assemblyCoverage.ClassCoverageDetails)
                {
                    classCoverage[@class.Key] = @class.Value;
                }
            }

            // Calculate overall coverage
            var totalLines = fileCoverage.Values.Sum(f => f.TotalLines);
            var coveredLines = fileCoverage.Values.Sum(f => f.CoveredLines);
            var totalBranches = fileCoverage.Values.Sum(f => f.TotalBranches);
            var coveredBranches = fileCoverage.Values.Sum(f => f.CoveredBranches);
            var totalMethods = classCoverage.Values.Sum(c => c.TotalMethods);
            var coveredMethods = classCoverage.Values.Sum(c => c.CoveredMethods);
            var totalClasses = classCoverage.Count;
            var coveredClasses = classCoverage.Values.Count(c => c.MethodCoverage > 0);

            var lineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;
            var branchCoverage = totalBranches > 0 ? (double)coveredBranches / totalBranches * 100 : 0;
            var methodCoverage = totalMethods > 0 ? (double)coveredMethods / totalMethods * 100 : 0;
            var classCoveragePercent = totalClasses > 0 ? (double)coveredClasses / totalClasses * 100 : 0;
            var overallCoverage = (lineCoverage + branchCoverage + methodCoverage + classCoveragePercent) / 4;

            return new TestCoverageInfo(
                overallCoverage,
                lineCoverage,
                branchCoverage,
                methodCoverage,
                classCoveragePercent,
                totalLines,
                coveredLines,
                totalBranches,
                coveredBranches,
                totalMethods,
                coveredMethods,
                totalClasses,
                coveredClasses,
                fileCoverage,
                classCoverage
            );
        }

        private async Task<TestCoverageInfo> AnalyzeAssemblyAsync(
            Assembly assembly,
            List<Assembly> testAssemblies,
            CancellationToken cancellationToken)
        {
            var fileCoverage = new Dictionary<string, FileCoverageInfo>();
            var classCoverage = new Dictionary<string, ClassCoverageInfo>();

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                foreach (var type in types)
                {
                    var typeCoverage = await AnalyzeTypeAsync(type, testAssemblies, cancellationToken);
                    if (typeCoverage != null)
                    {
                        var key = $"{type.Namespace}.{type.Name}";
                        classCoverage[key] = typeCoverage;
                    }
                }

                // Create file coverage from class coverage
                foreach (var @class in classCoverage.Values)
                {
                    var fileName = $"{@class.ClassName}.cs"; // Simplified file name
                    if (!fileCoverage.ContainsKey(fileName))
                    {
                        fileCoverage[fileName] = new FileCoverageInfo(
                            fileName,
                            @class.LineCoverage,
                            0, // Branch coverage not calculated in this simplified version
                            @class.TotalLines,
                            @class.CoveredLines,
                            0, // Total branches
                            0, // Covered branches
                            new List<int>()
                        );
                    }
                }

                return new TestCoverageInfo(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, fileCoverage, classCoverage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze assembly: {AssemblyName}", assembly.GetName().Name);
                return CreateEmptyCoverageInfo();
            }
        }
    }
}
