using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Base class for AI-enhanced agents, providing core functionality for AI processing, task analysis, and suggestion generation.
    /// This class is split into partial files for better organization:
    /// - BaseAIEnhancedAgent.Core.cs - Core properties and initialization
    /// - BaseAIEnhancedAgent.RequestProcessing.cs - Core request processing methods
    /// - BaseAIEnhancedAgent.AIProcessing.cs - AI-enhanced request processing
    /// - BaseAIEnhancedAgent.TaskAnalysis.cs - AI task analysis functionality
    /// - BaseAIEnhancedAgent.Suggestions.cs - AI suggestion generation
    /// - BaseAIEnhancedAgent.Lifecycle.cs - Agent start/stop lifecycle
    /// - BaseAIEnhancedAgent.Prompts.cs - AI prompt generation methods
    /// - BaseAIEnhancedAgent.Parsing.cs - AI response parsing methods
    /// - BaseAIEnhancedAgent.Utilities.cs - Helper methods like keyword extraction
    /// </summary>
    public abstract partial class BaseAiEnhancedAgent : IAiEnhancedAgent
    {
        // This abstract class is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }
}
