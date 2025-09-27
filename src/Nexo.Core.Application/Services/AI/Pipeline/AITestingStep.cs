using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered test generation pipeline step for automatic test case creation.
    /// This class is split into partial files for better organization:
    /// - AITestingStep.Core.cs - Main pipeline execution logic
    /// - AITestingStep.TestGeneration.cs - AI-powered test code generation
    /// - AITestingStep.Enhancement.cs - Test code enhancement and utilities
    /// - AITestingStep.Safety.cs - Safety validation and filtering
    /// - AITestingStep.Quality.cs - Test quality and coverage calculation
    /// - AITestingStep.Utilities.cs - Helper methods and type conversions
    /// </summary>
    public partial class AITestingStep : IPipelineStep<TestingRequest>
    {
        // This class is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }

    /// <summary>
    /// Testing result from AI pipeline processing
    /// </summary>
    public class TestingResult
    {
        public string GeneratedTests { get; set; } = string.Empty;
        public TestType TestType { get; set; }
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public DateTime GenerationTime { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public AIEngineType EngineType { get; set; }
        public List<string> TestCategories { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of tests
    /// </summary>
    public enum TestType
    {
        Unit,
        Integration,
        Performance,
        Security,
        EndToEnd
    }
}
