using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Test dependency graph functionality
    /// </summary>
    public partial class TestImpactAnalyzer
    {
        public async Task<TestDependencyGraph> BuildDependencyGraphAsync(string projectRoot, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Building test dependency graph for: {ProjectRoot}", projectRoot);

            // Check cache first
            if (_dependencyGraphCache.TryGetValue(projectRoot, out var cachedGraph))
            {
                return cachedGraph;
            }

            var graph = new TestDependencyGraph();
            var testFiles = await DiscoverTestFilesAsync(projectRoot, cancellationToken);

            try
            {
                // Build nodes
                foreach (var testFile in testFiles)
                {
                    var node = await CreateTestNodeAsync(testFile, cancellationToken);
                    graph.Nodes.Add(node);
                }

                // Build edges (simplified for now)
                foreach (var node in graph.Nodes)
                {
                    var dependencies = await FindTestDependenciesAsync(node, graph.Nodes, cancellationToken);
                    foreach (var dependency in dependencies)
                    {
                        graph.Edges.Add(new TestEdge
                        {
                            FromFilePath = node.FilePath,
                            ToFilePath = dependency.FilePath,
                            DependencyType = TestDependencyType.Direct,
                            Strength = 0.8
                        });
                    }
                }

                // Cache the graph
                _dependencyGraphCache[projectRoot] = graph;

                _logger.LogDebug("Built dependency graph with {Nodes} nodes and {Edges} edges", 
                    graph.Nodes.Count, graph.Edges.Count);

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building test dependency graph");
                return new TestDependencyGraph();
            }
        }

        private Task<TestNode> CreateTestNodeAsync(string testFile, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestNode
            {
                FilePath = testFile,
                ProjectName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(testFile) ?? ""),
                TestFramework = DetermineTestFramework(testFile),
                EstimatedExecutionTimeMs = 1000, // Default estimate
                Priority = 1,
                Categories = new List<string>()
            });
        }

        private string DetermineTestFramework(string testFile)
        {
            try
            {
                var content = File.ReadAllText(testFile);
                if (content.Contains("xunit"))
                    return "xUnit";
                if (content.Contains("nunit"))
                    return "NUnit";
                if (content.Contains("mstest"))
                    return "MSTest";
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private Task<List<TestNode>> FindTestDependenciesAsync(TestNode node, List<TestNode> allNodes, CancellationToken cancellationToken)
        {
            // Simplified dependency detection - in practice, you'd analyze test content
            return Task.FromResult(new List<TestNode>());
        }
    }
}
