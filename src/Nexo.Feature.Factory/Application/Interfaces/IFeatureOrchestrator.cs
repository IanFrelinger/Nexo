using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Models;

namespace Nexo.Feature.Factory.Application.Interfaces
{
    /// <summary>
    /// Orchestrates the entire feature generation process from natural language to production-ready code.
    /// This is the main entry point for the AI-native feature factory.
    /// </summary>
    public interface IFeatureOrchestrator
    {
        /// <summary>
        /// Generates a complete feature from a natural language description.
        /// </summary>
        /// <param name="description">The natural language description of the feature</param>
        /// <param name="targetPlatform">The target platform for code generation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated feature specification with all artifacts</returns>
        Task<FeatureGenerationResult> GenerateFeatureAsync(string description, Domain.Enums.TargetPlatform targetPlatform, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a feature from an existing specification.
        /// </summary>
        /// <param name="specification">The feature specification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated feature artifacts</returns>
        Task<FeatureGenerationResult> GenerateFeatureAsync(FeatureSpecification specification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes a natural language description and creates a feature specification.
        /// </summary>
        /// <param name="description">The natural language description</param>
        /// <param name="targetPlatform">The target platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The analyzed feature specification</returns>
        Task<FeatureSpecification> AnalyzeFeatureAsync(string description, Domain.Enums.TargetPlatform targetPlatform, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of feature generation.
    /// </summary>
    public sealed class FeatureGenerationResult
    {
        /// <summary>
        /// Gets the feature specification that was generated.
        /// </summary>
        public FeatureSpecification Specification { get; }

        /// <summary>
        /// Gets the generated code artifacts.
        /// </summary>
        public IReadOnlyList<CodeArtifact> CodeArtifacts { get; }

        /// <summary>
        /// Gets the generation metadata.
        /// </summary>
        public GenerationMetadata Metadata { get; }

        /// <summary>
        /// Gets whether the generation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets any error messages if generation failed.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        public FeatureGenerationResult(FeatureSpecification specification, IReadOnlyList<CodeArtifact> codeArtifacts, GenerationMetadata metadata, bool isSuccess, IReadOnlyList<string>? errors = null)
        {
            Specification = specification;
            CodeArtifacts = codeArtifacts ?? new List<CodeArtifact>();
            Metadata = metadata;
            IsSuccess = isSuccess;
            Errors = errors ?? new List<string>();
        }
    }

    /// <summary>
    /// Represents a generated code artifact.
    /// </summary>
    public sealed class CodeArtifact
    {
        /// <summary>
        /// Gets the name of the artifact.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the artifact.
        /// </summary>
        public ArtifactType Type { get; }

        /// <summary>
        /// Gets the content of the artifact.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the file path where the artifact should be placed.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the namespace for the artifact.
        /// </summary>
        public string? Namespace { get; }

        public CodeArtifact(string name, ArtifactType type, string content, string filePath, string? @namespace = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Namespace = @namespace;
        }
    }

    /// <summary>
    /// Represents the type of code artifact.
    /// </summary>
    public enum ArtifactType
    {
        Entity,
        ValueObject,
        Repository,
        UseCase,
        Test,
        Configuration,
        Documentation
    }

    /// <summary>
    /// Represents metadata about the generation process.
    /// </summary>
    public sealed class GenerationMetadata
    {
        /// <summary>
        /// Gets the generation start time.
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the generation end time.
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        /// Gets the total generation duration.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Gets the number of agents involved in generation.
        /// </summary>
        public int AgentsUsed { get; }

        /// <summary>
        /// Gets the execution strategy used.
        /// </summary>
        public Domain.Enums.ExecutionStrategy ExecutionStrategy { get; }

        public GenerationMetadata(DateTimeOffset startTime, DateTimeOffset endTime, int agentsUsed, Domain.Enums.ExecutionStrategy executionStrategy)
        {
            StartTime = startTime;
            EndTime = endTime;
            AgentsUsed = agentsUsed;
            ExecutionStrategy = executionStrategy;
        }
    }
}
