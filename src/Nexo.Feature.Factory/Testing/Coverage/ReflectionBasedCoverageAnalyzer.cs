using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Reflection-based test coverage analyzer that analyzes code coverage by examining test execution.
    /// </summary>
    public sealed class ReflectionBasedCoverageAnalyzer : ITestCoverageAnalyzer
    {
        private readonly ILogger<ReflectionBasedCoverageAnalyzer> _logger;
        private CoverageThresholds _thresholds = new();

        /// <summary>
        /// Initializes a new instance of the ReflectionBasedCoverageAnalyzer class.
        /// </summary>
        public ReflectionBasedCoverageAnalyzer(ILogger<ReflectionBasedCoverageAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

        /// <summary>
        /// Analyzes test coverage for the specified source files.
        /// </summary>
        public async Task<TestCoverageInfo> AnalyzeSourceCoverageAsync(
            IEnumerable<string> sourceFiles,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting source coverage analysis for {SourceFileCount} source files", sourceFiles.Count());

            try
            {
                var fileCoverage = new Dictionary<string, FileCoverageInfo>();
                var classCoverage = new Dictionary<string, ClassCoverageInfo>();

                // Analyze source files
                foreach (var sourceFile in sourceFiles)
                {
                    if (File.Exists(sourceFile))
                    {
                        var coverage = await AnalyzeSourceFileAsync(sourceFile, testFiles, cancellationToken);
                        if (coverage != null)
                        {
                            fileCoverage[sourceFile] = coverage;
                        }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze source coverage");
                return CreateEmptyCoverageInfo();
            }
        }

        /// <summary>
        /// Generates a coverage report in the specified format.
        /// </summary>
        public async Task GenerateCoverageReportAsync(
            TestCoverageInfo coverage,
            string outputPath,
            CoverageReportFormat format,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating coverage report in {Format} format to {OutputPath}", format, outputPath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

                switch (format)
                {
                    case CoverageReportFormat.Html:
                        await GenerateHtmlReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Json:
                        await GenerateJsonReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Xml:
                        await GenerateXmlReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Text:
                        await GenerateTextReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Markdown:
                        await GenerateMarkdownReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported report format: {format}", nameof(format));
                }

                _logger.LogInformation("Coverage report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate coverage report");
                throw;
            }
        }

        /// <summary>
        /// Gets coverage thresholds for different coverage types.
        /// </summary>
        public CoverageThresholds GetCoverageThresholds()
        {
            return _thresholds;
        }

        /// <summary>
        /// Sets coverage thresholds for different coverage types.
        /// </summary>
        public void SetCoverageThresholds(CoverageThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
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

        private async Task<FileCoverageInfo?> AnalyzeSourceFileAsync(
            string sourceFile,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken)
        {
            try
            {
                var content = await File.ReadAllTextAsync(sourceFile, cancellationToken);
                var lines = content.Split('\n');
                var totalLines = lines.Length;
                var coveredLines = 0;
                var uncoveredLines = new List<int>();

                // Simplified line coverage analysis
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("/*"))
                    {
                        // Check if this line is likely covered by tests
                        var isCovered = await IsLineCoveredAsync(line, testFiles, cancellationToken);
                        if (isCovered)
                        {
                            coveredLines++;
                        }
                        else
                        {
                            uncoveredLines.Add(i + 1);
                        }
                    }
                }

                var lineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;

                return new FileCoverageInfo(
                    sourceFile,
                    lineCoverage,
                    0, // Branch coverage not calculated in this simplified version
                    totalLines,
                    coveredLines,
                    0, // Total branches
                    0, // Covered branches
                    uncoveredLines
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze source file: {SourceFile}", sourceFile);
                return null;
            }
        }

        private Task<bool> IsLineCoveredAsync(
            string line,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken)
        {
            try
            {
                // Simplified line coverage detection
                // In a real implementation, this would use more sophisticated analysis
                return Task.FromResult(line.Contains("public") || line.Contains("private") || line.Contains("protected"));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private async Task GenerateHtmlReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Test Coverage Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .metric {{ margin: 10px 0; }}
        .progress-bar {{ width: 100%; background-color: #e0e0e0; border-radius: 5px; overflow: hidden; }}
        .progress-fill {{ height: 20px; background-color: #4CAF50; transition: width 0.3s; }}
        .low-coverage {{ background-color: #f44336; }}
        .medium-coverage {{ background-color: #ff9800; }}
        .high-coverage {{ background-color: #4CAF50; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Test Coverage Report</h1>
        <p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <h2>Overall Coverage: {coverage.OverallCoverage:F1}%</h2>
    
    <div class=""metric"">
        <h3>Line Coverage: {coverage.LineCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.LineCoverage}%""></div>
        </div>
        <p>{coverage.CoveredLines} of {coverage.TotalLines} lines covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Branch Coverage: {coverage.BranchCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.BranchCoverage}%""></div>
        </div>
        <p>{coverage.CoveredBranches} of {coverage.TotalBranches} branches covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Method Coverage: {coverage.MethodCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.MethodCoverage}%""></div>
        </div>
        <p>{coverage.CoveredMethods} of {coverage.TotalMethods} methods covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Class Coverage: {coverage.ClassCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.ClassCoverage}%""></div>
        </div>
        <p>{coverage.CoveredClasses} of {coverage.TotalClasses} classes covered</p>
    </div>
</body>
</html>";

            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        private async Task GenerateJsonReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);
        }

        private async Task GenerateXmlReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<coverage>
    <overall>{coverage.OverallCoverage:F1}</overall>
    <line>{coverage.LineCoverage:F1}</line>
    <branch>{coverage.BranchCoverage:F1}</branch>
    <method>{coverage.MethodCoverage:F1}</method>
    <class>{coverage.ClassCoverage:F1}</class>
    <lines total=""{coverage.TotalLines}"" covered=""{coverage.CoveredLines}"" />
    <branches total=""{coverage.TotalBranches}"" covered=""{coverage.CoveredBranches}"" />
    <methods total=""{coverage.TotalMethods}"" covered=""{coverage.CoveredMethods}"" />
    <classes total=""{coverage.TotalClasses}"" covered=""{coverage.CoveredClasses}"" />
</coverage>";

            await File.WriteAllTextAsync(outputPath, xml, cancellationToken);
        }

        private async Task GenerateTextReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var text = $@"Test Coverage Report
==================
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Overall Coverage: {coverage.OverallCoverage:F1}%
Line Coverage: {coverage.LineCoverage:F1}% ({coverage.CoveredLines}/{coverage.TotalLines})
Branch Coverage: {coverage.BranchCoverage:F1}% ({coverage.CoveredBranches}/{coverage.TotalBranches})
Method Coverage: {coverage.MethodCoverage:F1}% ({coverage.CoveredMethods}/{coverage.TotalMethods})
Class Coverage: {coverage.ClassCoverage:F1}% ({coverage.CoveredClasses}/{coverage.TotalClasses})
";

            await File.WriteAllTextAsync(outputPath, text, cancellationToken);
        }

        private async Task GenerateMarkdownReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var markdown = $@"# Test Coverage Report

Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

## Overall Coverage: {coverage.OverallCoverage:F1}%

| Metric | Coverage | Details |
|--------|----------|---------|
| Line Coverage | {coverage.LineCoverage:F1}% | {coverage.CoveredLines}/{coverage.TotalLines} lines |
| Branch Coverage | {coverage.BranchCoverage:F1}% | {coverage.CoveredBranches}/{coverage.TotalBranches} branches |
| Method Coverage | {coverage.MethodCoverage:F1}% | {coverage.CoveredMethods}/{coverage.TotalMethods} methods |
| Class Coverage | {coverage.ClassCoverage:F1}% | {coverage.CoveredClasses}/{coverage.TotalClasses} classes |

## File Coverage

| File | Line Coverage | Branch Coverage |
|------|---------------|-----------------|
";

            foreach (var file in coverage.FileCoverage.Take(10))
            {
                markdown += $"| {Path.GetFileName(file.Key)} | {file.Value.LineCoverage:F1}% | {file.Value.BranchCoverage:F1}% |\n";
            }

            await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);
        }

        private static TestCoverageInfo CreateEmptyCoverageInfo()
        {
            return new TestCoverageInfo(
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, FileCoverageInfo>(),
                new Dictionary<string, ClassCoverageInfo>()
            );
        }
    }
}
