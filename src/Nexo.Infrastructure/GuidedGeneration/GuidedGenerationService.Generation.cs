using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Tool generation functionality for GuidedGenerationService.
    /// </summary>
    public partial class GuidedGenerationService
    {
        /// <summary>
        /// Generates the tool based on session data.
        /// </summary>
        private async Task<GeneratedTool> GenerateToolInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    throw new InvalidOperationException("Session not found");
                }

                if (session.Status != GenerationStatus.Completed)
                {
                    throw new InvalidOperationException("Session is not completed");
                }

                _logger.LogDebug("Generating tool for session: {SessionId}", sessionId);

                // Build description from session data
                var description = BuildDescriptionFromSession(session);
                
                // Generate tool
                var tool = await _orchestrator.GenerateToolAsync(description, cancellationToken);
                
                // Store results
                session.GeneratedCode = tool.QualityScore?.ToString();
                session.QualityScore = tool.QualityScore;
                session.Status = GenerationStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Tool generated successfully for session: {SessionId}, tool: {ToolName}", sessionId, tool.Name);
                return tool;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tool for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Builds a description from session data.
        /// </summary>
        private string BuildDescriptionFromSession(GenerationSession session)
        {
            var description = $"Create a tool called '{session.ToolName}'";
            
            if (!string.IsNullOrEmpty(session.Category))
            {
                description += $" in the {session.Category} category";
            }
            
            description += $". {session.Description}";
            
            if (session.Inputs.Any())
            {
                description += $" The tool should accept these inputs: {string.Join(", ", session.Inputs)}.";
            }
            
            if (session.Outputs.Any())
            {
                description += $" The tool should produce these outputs: {string.Join(", ", session.Outputs)}.";
            }
            
            if (session.Requirements.Any())
            {
                description += $" Additional requirements: {string.Join(", ", session.Requirements)}.";
            }
            
            return description;
        }
    }
}
