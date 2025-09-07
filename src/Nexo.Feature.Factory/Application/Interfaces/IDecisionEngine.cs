using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Interfaces
{
    /// <summary>
    /// AI-powered decision engine that analyzes feature requirements and chooses the optimal execution strategy.
    /// </summary>
    public interface IDecisionEngine
    {
        /// <summary>
        /// Determines the optimal execution strategy for a feature specification.
        /// </summary>
        /// <param name="specification">The feature specification to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The recommended execution strategy with reasoning</returns>
        Task<ExecutionStrategyDecision> DetermineStrategyAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes the complexity of a feature specification.
        /// </summary>
        /// <param name="specification">The feature specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The complexity analysis</returns>
        Task<ComplexityAnalysis> AnalyzeComplexityAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates the performance requirements for a feature specification.
        /// </summary>
        /// <param name="specification">The feature specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The performance requirements analysis</returns>
        Task<PerformanceAnalysis> AnalyzePerformanceRequirementsAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines the platform-specific optimizations needed.
        /// </summary>
        /// <param name="specification">The feature specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The platform optimization recommendations</returns>
        Task<PlatformOptimizationRecommendation> AnalyzePlatformOptimizationsAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a decision about execution strategy.
    /// </summary>
    public sealed class ExecutionStrategyDecision
    {
        /// <summary>
        /// Gets the recommended execution strategy.
        /// </summary>
        public ExecutionStrategy Strategy { get; }

        /// <summary>
        /// Gets the confidence level of the decision (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; }

        /// <summary>
        /// Gets the reasoning behind the decision.
        /// </summary>
        public string Reasoning { get; }

        /// <summary>
        /// Gets the factors that influenced the decision.
        /// </summary>
        public IReadOnlyList<DecisionFactor> Factors { get; }

        public ExecutionStrategyDecision(ExecutionStrategy strategy, double confidence, string reasoning, IReadOnlyList<DecisionFactor> factors)
        {
            Strategy = strategy;
            Confidence = confidence;
            Reasoning = reasoning ?? throw new ArgumentNullException(nameof(reasoning));
            Factors = factors ?? new List<DecisionFactor>();
        }
    }

    /// <summary>
    /// Represents a factor that influenced a decision.
    /// </summary>
    public sealed class DecisionFactor
    {
        /// <summary>
        /// Gets the name of the factor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the weight of the factor (0.0 to 1.0).
        /// </summary>
        public double Weight { get; }

        /// <summary>
        /// Gets the impact of the factor on the decision.
        /// </summary>
        public FactorImpact Impact { get; }

        /// <summary>
        /// Gets the description of the factor.
        /// </summary>
        public string Description { get; }

        public DecisionFactor(string name, double weight, FactorImpact impact, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Weight = weight;
            Impact = impact;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Represents the impact of a decision factor.
    /// </summary>
    public enum FactorImpact
    {
        Positive,
        Negative,
        Neutral
    }

    /// <summary>
    /// Represents a complexity analysis of a feature specification.
    /// </summary>
    public sealed class ComplexityAnalysis
    {
        /// <summary>
        /// Gets the overall complexity score (0.0 to 1.0).
        /// </summary>
        public double OverallComplexity { get; }

        /// <summary>
        /// Gets the domain complexity score.
        /// </summary>
        public double DomainComplexity { get; }

        /// <summary>
        /// Gets the technical complexity score.
        /// </summary>
        public double TechnicalComplexity { get; }

        /// <summary>
        /// Gets the integration complexity score.
        /// </summary>
        public double IntegrationComplexity { get; }

        /// <summary>
        /// Gets the complexity factors.
        /// </summary>
        public IReadOnlyList<ComplexityFactor> Factors { get; }

        public ComplexityAnalysis(double overallComplexity, double domainComplexity, double technicalComplexity, double integrationComplexity, IReadOnlyList<ComplexityFactor> factors)
        {
            OverallComplexity = overallComplexity;
            DomainComplexity = domainComplexity;
            TechnicalComplexity = technicalComplexity;
            IntegrationComplexity = integrationComplexity;
            Factors = factors ?? new List<ComplexityFactor>();
        }
    }

    /// <summary>
    /// Represents a complexity factor.
    /// </summary>
    public sealed class ComplexityFactor
    {
        /// <summary>
        /// Gets the name of the factor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the complexity score for this factor.
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the description of the factor.
        /// </summary>
        public string Description { get; }

        public ComplexityFactor(string name, double score, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Score = score;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Represents a performance analysis of a feature specification.
    /// </summary>
    public sealed class PerformanceAnalysis
    {
        /// <summary>
        /// Gets the performance requirements level.
        /// </summary>
        public PerformanceLevel Level { get; }

        /// <summary>
        /// Gets the expected throughput requirements.
        /// </summary>
        public ThroughputRequirements Throughput { get; }

        /// <summary>
        /// Gets the latency requirements.
        /// </summary>
        public LatencyRequirements Latency { get; }

        /// <summary>
        /// Gets the scalability requirements.
        /// </summary>
        public ScalabilityRequirements Scalability { get; }

        public PerformanceAnalysis(PerformanceLevel level, ThroughputRequirements throughput, LatencyRequirements latency, ScalabilityRequirements scalability)
        {
            Level = level;
            Throughput = throughput;
            Latency = latency;
            Scalability = scalability;
        }
    }

    /// <summary>
    /// Represents the performance level.
    /// </summary>
    public enum PerformanceLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents throughput requirements.
    /// </summary>
    public sealed class ThroughputRequirements
    {
        public int RequestsPerSecond { get; }
        public string Description { get; }

        public ThroughputRequirements(int requestsPerSecond, string description)
        {
            RequestsPerSecond = requestsPerSecond;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Represents latency requirements.
    /// </summary>
    public sealed class LatencyRequirements
    {
        public TimeSpan MaxLatency { get; }
        public string Description { get; }

        public LatencyRequirements(TimeSpan maxLatency, string description)
        {
            MaxLatency = maxLatency;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Represents scalability requirements.
    /// </summary>
    public sealed class ScalabilityRequirements
    {
        public int MaxConcurrentUsers { get; }
        public string Description { get; }

        public ScalabilityRequirements(int maxConcurrentUsers, string description)
        {
            MaxConcurrentUsers = maxConcurrentUsers;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Represents platform optimization recommendations.
    /// </summary>
    public sealed class PlatformOptimizationRecommendation
    {
        /// <summary>
        /// Gets the target platform.
        /// </summary>
        public TargetPlatform Platform { get; }

        /// <summary>
        /// Gets the optimization recommendations.
        /// </summary>
        public IReadOnlyList<OptimizationRecommendation> Recommendations { get; }

        public PlatformOptimizationRecommendation(TargetPlatform platform, IReadOnlyList<OptimizationRecommendation> recommendations)
        {
            Platform = platform;
            Recommendations = recommendations ?? new List<OptimizationRecommendation>();
        }
    }

    /// <summary>
    /// Represents an optimization recommendation.
    /// </summary>
    public sealed class OptimizationRecommendation
    {
        /// <summary>
        /// Gets the type of optimization.
        /// </summary>
        public OptimizationType Type { get; }

        /// <summary>
        /// Gets the description of the recommendation.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the expected impact.
        /// </summary>
        public string Impact { get; }

        public OptimizationRecommendation(OptimizationType type, string description, string impact)
        {
            Type = type;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Impact = impact ?? throw new ArgumentNullException(nameof(impact));
        }
    }

    /// <summary>
    /// Represents the type of optimization.
    /// </summary>
    public enum OptimizationType
    {
        Performance,
        Memory,
        Network,
        Caching,
        Database,
        Security
    }
}
