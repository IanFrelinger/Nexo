using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.Unity.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Data models and interfaces for UnityCommands.
    /// </summary>
    public static partial class UnityCommands
    {
        // This partial class contains data models and interfaces
        // The actual models are defined below
    }

    /// <summary>
    /// Unity project optimizer interface
    /// </summary>
    public interface IUnityProjectOptimizer
    {
        Task<UnityOptimizationResult> OptimizeProjectAsync(UnityOptimizationRequest request);
    }

    /// <summary>
    /// Unity optimization request
    /// </summary>
    public class UnityOptimizationRequest
    {
        public string ProjectPath { get; set; } = string.Empty;
        public UnityOptimizationTarget OptimizationTarget { get; set; }
        public bool ApplyOptimizations { get; set; }
    }

    /// <summary>
    /// Unity optimization result
    /// </summary>
    public class UnityOptimizationResult
    {
        public bool Success { get; set; }
        public IEnumerable<OptimizationImprovement> Improvements { get; set; } = new List<OptimizationImprovement>();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimization improvement
    /// </summary>
    public class OptimizationImprovement
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double ImprovementFactor { get; set; }
    }

    /// <summary>
    /// Unity optimization target
    /// </summary>
    public enum UnityOptimizationTarget
    {
        Performance,
        Memory,
        BuildSize,
        All
    }
}
