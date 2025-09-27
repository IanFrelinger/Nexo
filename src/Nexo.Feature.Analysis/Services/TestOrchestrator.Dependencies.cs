using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    public partial class TestOrchestrator
    {
        public async Task<TestExecutionPlan> CreateDependencyOrderedPlanAsync(List<string> testFiles, DependencyOrderingOptions options, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating dependency-ordered execution plan for {Count} test files", testFiles.Count);

            try
            {
                var dependencies = new List<TestDependencyRule>();

                // Auto-detect dependencies if enabled
                if (options.AutoDetectDependencies)
                {
                    var detectedDependencies = await _testDependencyAnalyzer.AnalyzeDependenciesAsync(testFiles, cancellationToken);
                    dependencies.AddRange(detectedDependencies);
                }

                // Add custom dependencies
                dependencies.AddRange(options.CustomDependencies);

                // Validate for cycles if enabled
                if (options.ValidateCycles)
                {
                    var cycles = DetectDependencyCycles(dependencies);
                    if (cycles.Any())
                    {
                        return new TestExecutionPlan
                        {
                            IsValid = false,
                            ValidationErrors = cycles.Select(c => $"Circular dependency detected: {string.Join(" -> ", c)}").ToList()
                        };
                    }
                }

                // Create phases based on dependencies
                var phases = CreatePhasesFromDependencies(testFiles, dependencies, options);

                var totalEstimatedTime = TimeSpan.FromMinutes(phases.Sum(p => p.EstimatedTime.TotalMinutes));
                var dependencyGraph = GenerateDependencyGraph(dependencies);

                var plan = new TestExecutionPlan
                {
                    Phases = phases,
                    TotalTests = testFiles.Count,
                    EstimatedExecutionTime = totalEstimatedTime,
                    DependencyGraph = dependencyGraph,
                    IsValid = true
                };

                _logger.LogInformation("Dependency-ordered plan created with {Phases} phases, estimated time: {EstimatedTime}",
                    phases.Count, totalEstimatedTime);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dependency-ordered plan");
                return new TestExecutionPlan
                {
                    IsValid = false,
                    ValidationErrors = new List<string> { ex.Message }
                };
            }
        }

        private List<List<string>> DetectDependencyCycles(List<TestDependencyRule> dependencies)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency.DependentTest))
                {
                    var cycle = new List<string>();
                    if (HasCycle(dependency.DependentTest, dependencies, visited, recursionStack, cycle))
                    {
                        cycles.Add(new List<string>(cycle));
                    }
                }
            }

            return cycles;
        }

        private bool HasCycle(string test, List<TestDependencyRule> dependencies, HashSet<string> visited, HashSet<string> recursionStack, List<string> cycle)
        {
            if (recursionStack.Contains(test))
            {
                cycle.Add(test);
                return true;
            }

            if (visited.Contains(test))
            {
                return false;
            }

            visited.Add(test);
            recursionStack.Add(test);
            cycle.Add(test);

            var dependents = dependencies.Where(d => d.DependencyTest == test).Select(d => d.DependentTest);
            foreach (var dependent in dependents)
            {
                if (HasCycle(dependent, dependencies, visited, recursionStack, cycle))
                {
                    return true;
                }
            }

            recursionStack.Remove(test);
            cycle.RemoveAt(cycle.Count - 1);
            return false;
        }

        private List<TestExecutionPhase> CreatePhasesFromDependencies(List<string> testFiles, List<TestDependencyRule> dependencies, DependencyOrderingOptions options)
        {
            var phases = new List<TestExecutionPhase>();
            var remainingTests = new HashSet<string>(testFiles);
            var phaseNumber = 1;

            while (remainingTests.Any())
            {
                var independentTests = GetIndependentTests(remainingTests, dependencies);
                var phaseTests = independentTests.Take(options.MaxGroupSize).ToList();

                var phase = new TestExecutionPhase
                {
                    Id = $"phase-{phaseNumber}",
                    Name = $"Phase {phaseNumber}",
                    TestFiles = phaseTests,
                    CanRunInParallel = options.GroupIndependentTests,
                    EstimatedTime = TimeSpan.FromMinutes(phaseTests.Count * 2) // Rough estimate
                };

                phases.Add(phase);

                foreach (var test in phaseTests)
                {
                    remainingTests.Remove(test);
                }

                phaseNumber++;
            }

            return phases;
        }

        private List<string> GetIndependentTests(HashSet<string> remainingTests, List<TestDependencyRule> dependencies)
        {
            var dependentTests = new HashSet<string>(dependencies
                .Where(d => remainingTests.Contains(d.DependentTest))
                .Select(d => d.DependentTest));

            return remainingTests.Where(test => !dependentTests.Contains(test)).ToList();
        }

        private string GenerateDependencyGraph(List<TestDependencyRule> dependencies)
        {
            // Simple text-based dependency graph
            var graph = new List<string>();
            foreach (var dependency in dependencies)
            {
                graph.Add($"{dependency.DependencyTest} -> {dependency.DependentTest}");
            }
            return string.Join("\n", graph);
        }
    }
}
