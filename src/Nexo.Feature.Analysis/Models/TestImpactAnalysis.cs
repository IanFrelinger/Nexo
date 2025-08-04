using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Result of test impact analysis.
    /// </summary>
    public class TestImpactAnalysis
    {
        /// <summary>
        /// List of test files that should be run based on changes.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();

        /// <summary>
        /// List of all test files in the project.
        /// </summary>
        public List<string> AllTests { get; set; } = new List<string>();

        /// <summary>
        /// Confidence level of the analysis (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Number of tests selected vs total tests.
        /// </summary>
        public double SelectionRatio => AllTests.Count > 0 ? (double)AffectedTests.Count / AllTests.Count : 0.0;

        /// <summary>
        /// Analysis metadata and reasoning.
        /// </summary>
        public TestImpactMetadata Metadata { get; set; } = new TestImpactMetadata();

        /// <summary>
        /// Detailed mapping of source files to affected tests.
        /// </summary>
        public List<SourceTestMapping> SourceTestMappings { get; set; } = new List<SourceTestMapping>();
    }

    /// <summary>
    /// Metadata about the test impact analysis.
    /// </summary>
    public class TestImpactMetadata
    {
        /// <summary>
        /// Analysis strategy used.
        /// </summary>
        public string Strategy { get; set; } = string.Empty;

        /// <summary>
        /// Reasoning for test selection.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// Analysis duration in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Number of source files analyzed.
        /// </summary>
        public int SourceFilesAnalyzed { get; set; }

        /// <summary>
        /// Number of test files discovered.
        /// </summary>
        public int TestFilesDiscovered { get; set; }

        /// <summary>
        /// Warnings or issues encountered during analysis.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Mapping between a source file and its related tests.
    /// </summary>
    public class SourceTestMapping
    {
        /// <summary>
        /// Source file path.
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Related test file paths.
        /// </summary>
        public List<string> TestFiles { get; set; } = new List<string>();

        /// <summary>
        /// Confidence level for this mapping (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Mapping strategy used.
        /// </summary>
        public string MappingStrategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test dependency graph for analyzing test relationships.
    /// </summary>
    public class TestDependencyGraph
    {
        /// <summary>
        /// Nodes in the dependency graph (test files).
        /// </summary>
        public List<TestNode> Nodes { get; set; } = new List<TestNode>();

        /// <summary>
        /// Edges in the dependency graph (test relationships).
        /// </summary>
        public List<TestEdge> Edges { get; set; } = new List<TestEdge>();

        /// <summary>
        /// Gets a test node by file path.
        /// </summary>
        /// <param name="filePath">Test file path.</param>
        /// <returns>Test node or null if not found.</returns>
        public TestNode GetNode(string filePath)
        {
            return Nodes.FirstOrDefault(n => n.FilePath == filePath);
        }

        /// <summary>
        /// Gets dependencies for a test file.
        /// </summary>
        /// <param name="filePath">Test file path.</param>
        /// <returns>List of dependent test files.</returns>
        public List<string> GetDependencies(string filePath)
        {
            return Edges
                .Where(e => e.FromFilePath == filePath)
                .Select(e => e.ToFilePath)
                .ToList();
        }

        /// <summary>
        /// Gets dependents for a test file.
        /// </summary>
        /// <param name="filePath">Test file path.</param>
        /// <returns>List of test files that depend on this file.</returns>
        public List<string> GetDependents(string filePath)
        {
            return Edges
                .Where(e => e.ToFilePath == filePath)
                .Select(e => e.FromFilePath)
                .ToList();
        }
    }

    /// <summary>
    /// Node in the test dependency graph.
    /// </summary>
    public class TestNode
    {
        /// <summary>
        /// Test file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Test project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Test framework used.
        /// </summary>
        public string TestFramework { get; set; } = string.Empty;

        /// <summary>
        /// Estimated execution time in milliseconds.
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }

        /// <summary>
        /// Priority level for execution ordering.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Test categories or tags.
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();
    }

    /// <summary>
    /// Edge in the test dependency graph.
    /// </summary>
    public class TestEdge
    {
        /// <summary>
        /// Source test file path.
        /// </summary>
        public string FromFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Target test file path.
        /// </summary>
        public string ToFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Type of dependency relationship.
        /// </summary>
        public TestDependencyType DependencyType { get; set; }

        /// <summary>
        /// Strength of the dependency (0.0 to 1.0).
        /// </summary>
        public double Strength { get; set; }
    }

    /// <summary>
    /// Types of test dependencies.
    /// </summary>
    public enum TestDependencyType
    {
        /// <summary>
        /// Direct dependency (one test directly depends on another).
        /// </summary>
        Direct,

        /// <summary>
        /// Shared resource dependency (tests share common resources).
        /// </summary>
        SharedResource,

        /// <summary>
        /// Data dependency (tests share test data).
        /// </summary>
        Data,

        /// <summary>
        /// Infrastructure dependency (tests share infrastructure setup).
        /// </summary>
        Infrastructure,

        /// <summary>
        /// Indirect dependency (dependency through intermediate tests).
        /// </summary>
        Indirect
    }
}