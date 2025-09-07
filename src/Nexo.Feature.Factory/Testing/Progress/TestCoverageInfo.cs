using System.Collections.Generic;

namespace Nexo.Feature.Factory.Testing.Progress
{
    /// <summary>
    /// Represents test coverage information.
    /// </summary>
    public sealed class TestCoverageInfo
    {
        /// <summary>
        /// Gets the overall coverage percentage.
        /// </summary>
        public double OverallCoverage { get; }

        /// <summary>
        /// Gets the line coverage percentage.
        /// </summary>
        public double LineCoverage { get; }

        /// <summary>
        /// Gets the branch coverage percentage.
        /// </summary>
        public double BranchCoverage { get; }

        /// <summary>
        /// Gets the method coverage percentage.
        /// </summary>
        public double MethodCoverage { get; }

        /// <summary>
        /// Gets the class coverage percentage.
        /// </summary>
        public double ClassCoverage { get; }

        /// <summary>
        /// Gets the total number of lines.
        /// </summary>
        public int TotalLines { get; }

        /// <summary>
        /// Gets the number of covered lines.
        /// </summary>
        public int CoveredLines { get; }

        /// <summary>
        /// Gets the total number of branches.
        /// </summary>
        public int TotalBranches { get; }

        /// <summary>
        /// Gets the number of covered branches.
        /// </summary>
        public int CoveredBranches { get; }

        /// <summary>
        /// Gets the total number of methods.
        /// </summary>
        public int TotalMethods { get; }

        /// <summary>
        /// Gets the number of covered methods.
        /// </summary>
        public int CoveredMethods { get; }

        /// <summary>
        /// Gets the total number of classes.
        /// </summary>
        public int TotalClasses { get; }

        /// <summary>
        /// Gets the number of covered classes.
        /// </summary>
        public int CoveredClasses { get; }

        /// <summary>
        /// Gets the coverage details by file.
        /// </summary>
        public IReadOnlyDictionary<string, FileCoverageInfo> FileCoverage { get; }

        /// <summary>
        /// Gets the coverage details by class.
        /// </summary>
        public IReadOnlyDictionary<string, ClassCoverageInfo> ClassCoverageDetails { get; }

        /// <summary>
        /// Gets the timestamp when coverage was calculated.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the TestCoverageInfo class.
        /// </summary>
        public TestCoverageInfo(
            double overallCoverage,
            double lineCoverage,
            double branchCoverage,
            double methodCoverage,
            double classCoverage,
            int totalLines,
            int coveredLines,
            int totalBranches,
            int coveredBranches,
            int totalMethods,
            int coveredMethods,
            int totalClasses,
            int coveredClasses,
            IReadOnlyDictionary<string, FileCoverageInfo> fileCoverage,
            IReadOnlyDictionary<string, ClassCoverageInfo> classCoverageDetails)
        {
            OverallCoverage = overallCoverage;
            LineCoverage = lineCoverage;
            BranchCoverage = branchCoverage;
            MethodCoverage = methodCoverage;
            ClassCoverage = classCoverage;
            TotalLines = totalLines;
            CoveredLines = coveredLines;
            TotalBranches = totalBranches;
            CoveredBranches = coveredBranches;
            TotalMethods = totalMethods;
            CoveredMethods = coveredMethods;
            TotalClasses = totalClasses;
            CoveredClasses = coveredClasses;
            FileCoverage = fileCoverage ?? new Dictionary<string, FileCoverageInfo>();
            ClassCoverageDetails = classCoverageDetails ?? new Dictionary<string, ClassCoverageInfo>();
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Represents file-level coverage information.
    /// </summary>
    public sealed class FileCoverageInfo
    {
        /// <summary>
        /// Gets the file path.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the line coverage percentage for this file.
        /// </summary>
        public double LineCoverage { get; }

        /// <summary>
        /// Gets the branch coverage percentage for this file.
        /// </summary>
        public double BranchCoverage { get; }

        /// <summary>
        /// Gets the total number of lines in this file.
        /// </summary>
        public int TotalLines { get; }

        /// <summary>
        /// Gets the number of covered lines in this file.
        /// </summary>
        public int CoveredLines { get; }

        /// <summary>
        /// Gets the total number of branches in this file.
        /// </summary>
        public int TotalBranches { get; }

        /// <summary>
        /// Gets the number of covered branches in this file.
        /// </summary>
        public int CoveredBranches { get; }

        /// <summary>
        /// Gets the uncovered lines in this file.
        /// </summary>
        public IReadOnlyList<int> UncoveredLines { get; }

        /// <summary>
        /// Initializes a new instance of the FileCoverageInfo class.
        /// </summary>
        public FileCoverageInfo(
            string filePath,
            double lineCoverage,
            double branchCoverage,
            int totalLines,
            int coveredLines,
            int totalBranches,
            int coveredBranches,
            IReadOnlyList<int> uncoveredLines)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            LineCoverage = lineCoverage;
            BranchCoverage = branchCoverage;
            TotalLines = totalLines;
            CoveredLines = coveredLines;
            TotalBranches = totalBranches;
            CoveredBranches = coveredBranches;
            UncoveredLines = uncoveredLines ?? new List<int>();
        }
    }

    /// <summary>
    /// Represents class-level coverage information.
    /// </summary>
    public sealed class ClassCoverageInfo
    {
        /// <summary>
        /// Gets the class name.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the line coverage percentage for this class.
        /// </summary>
        public double LineCoverage { get; }

        /// <summary>
        /// Gets the method coverage percentage for this class.
        /// </summary>
        public double MethodCoverage { get; }

        /// <summary>
        /// Gets the total number of lines in this class.
        /// </summary>
        public int TotalLines { get; }

        /// <summary>
        /// Gets the number of covered lines in this class.
        /// </summary>
        public int CoveredLines { get; }

        /// <summary>
        /// Gets the total number of methods in this class.
        /// </summary>
        public int TotalMethods { get; }

        /// <summary>
        /// Gets the number of covered methods in this class.
        /// </summary>
        public int CoveredMethods { get; }

        /// <summary>
        /// Gets the uncovered methods in this class.
        /// </summary>
        public IReadOnlyList<string> UncoveredMethods { get; }

        /// <summary>
        /// Initializes a new instance of the ClassCoverageInfo class.
        /// </summary>
        public ClassCoverageInfo(
            string className,
            string @namespace,
            double lineCoverage,
            double methodCoverage,
            int totalLines,
            int coveredLines,
            int totalMethods,
            int coveredMethods,
            IReadOnlyList<string> uncoveredMethods)
        {
            ClassName = className ?? throw new ArgumentNullException(nameof(className));
            Namespace = @namespace ?? string.Empty;
            LineCoverage = lineCoverage;
            MethodCoverage = methodCoverage;
            TotalLines = totalLines;
            CoveredLines = coveredLines;
            TotalMethods = totalMethods;
            CoveredMethods = coveredMethods;
            UncoveredMethods = uncoveredMethods ?? new List<string>();
        }
    }
}
