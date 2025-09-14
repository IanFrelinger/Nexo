using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.Orchestration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Service for guided tool generation
    /// </summary>
    public class GuidedGenerationService : IGuidedGenerationService
    {
        private readonly ToolGenerationOrchestrator _orchestrator;
        private readonly ILogger<GuidedGenerationService> _logger;
        private readonly Dictionary<string, GenerationSession> _sessions = new();

        public GuidedGenerationService(
            ToolGenerationOrchestrator orchestrator,
            ILogger<GuidedGenerationService> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<GenerationSession> StartSessionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting new guided generation session");

                var session = new GenerationSession
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Status = GenerationStatus.InProgress
                };

                // Initialize steps
                session.Steps = CreateGenerationSteps();
                session.CurrentStepIndex = 0;

                _sessions[session.Id] = session;

                _logger.LogInformation("Started guided generation session: {SessionId}", session.Id);
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting guided generation session");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<GenerationStep?> GetNextStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
                if (session == null || !session.CanProceed)
                {
                    return null;
                }

                return await Task.FromResult(session.CurrentStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next step for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ValidationResult> SubmitStepInputAsync(string sessionId, string input, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
                if (session == null || !session.CanProceed)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Session not found or not in progress"
                    };
                }

                var currentStep = session.CurrentStep;
                if (currentStep == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "No current step available"
                    };
                }

                // Validate input
                var validationResult = currentStep.ValidationRule?.Validate(input) ?? new ValidationResult { IsValid = true };
                
                if (validationResult.IsValid)
                {
                    currentStep.UserInput = input;
                    currentStep.IsCompleted = true;
                    
                    // Store input based on step type
                    StoreStepInput(session, currentStep, input);
                    
                    _logger.LogDebug("Step input submitted for session: {SessionId}, step: {StepId}", sessionId, currentStep.Id);
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting step input for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> MoveToNextStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                var moved = session.MoveToNextStep();
                if (moved)
                {
                    _logger.LogDebug("Moved to next step for session: {SessionId}, step: {StepIndex}", sessionId, session.CurrentStepIndex);
                }

                return moved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to next step for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> MoveToPreviousStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                var moved = session.MoveToPreviousStep();
                if (moved)
                {
                    _logger.LogDebug("Moved to previous step for session: {SessionId}, step: {StepIndex}", sessionId, session.CurrentStepIndex);
                }

                return moved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to previous step for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<GeneratedTool> GenerateToolAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
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

        /// <inheritdoc />
        public async Task<GenerationSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _sessions.TryGetValue(sessionId, out var session);
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await GetSessionAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                session.Status = GenerationStatus.Cancelled;
                session.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Session cancelled: {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling session: {SessionId}", sessionId);
                throw;
            }
        }

        #region Private Methods

        private List<GenerationStep> CreateGenerationSteps()
        {
            return new List<GenerationStep>
            {
                new GenerationStep
                {
                    Order = 1,
                    Title = "Tool Name",
                    Description = "What would you like to call your tool?",
                    Question = "Enter a name for your tool:",
                    Type = StepType.TextInput,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Required,
                        MinLength = 3,
                        MaxLength = 50,
                        ErrorMessage = "Tool name must be between 3 and 50 characters"
                    },
                    Examples = new List<string> { "json-formatter", "api-tester", "csv-converter" }
                },
                new GenerationStep
                {
                    Order = 2,
                    Title = "Tool Category",
                    Description = "What type of tool is this?",
                    Question = "Select a category:",
                    Type = StepType.Choice,
                    IsRequired = true,
                    Options = new List<string>
                    {
                        "Data Processing",
                        "API Testing",
                        "File Conversion",
                        "Text Processing",
                        "Database Operations",
                        "System Utilities",
                        "Other"
                    },
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Choice,
                        AllowedValues = new List<string>
                        {
                            "Data Processing",
                            "API Testing",
                            "File Conversion",
                            "Text Processing",
                            "Database Operations",
                            "System Utilities",
                            "Other"
                        }
                    }
                },
                new GenerationStep
                {
                    Order = 3,
                    Title = "Tool Description",
                    Description = "What should this tool do?",
                    Question = "Describe what your tool should do:",
                    Type = StepType.TextArea,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Required,
                        MinLength = 10,
                        MaxLength = 500,
                        ErrorMessage = "Description must be between 10 and 500 characters"
                    },
                    Examples = new List<string>
                    {
                        "Convert CSV files to JSON format with customizable field mapping",
                        "Test REST API endpoints and generate detailed reports",
                        "Format and validate JSON data with pretty printing"
                    }
                },
                new GenerationStep
                {
                    Order = 4,
                    Title = "Inputs",
                    Description = "What inputs does your tool need?",
                    Question = "List the inputs your tool requires (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: file path, API URL, configuration options, etc."
                },
                new GenerationStep
                {
                    Order = 5,
                    Title = "Outputs",
                    Description = "What should your tool produce?",
                    Question = "List the outputs your tool should generate (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: formatted file, test report, processed data, etc."
                },
                new GenerationStep
                {
                    Order = 6,
                    Title = "Requirements",
                    Description = "Any specific requirements or constraints?",
                    Question = "List any specific requirements (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: must work with large files, needs error handling, requires specific libraries, etc."
                },
                new GenerationStep
                {
                    Order = 7,
                    Title = "Confirmation",
                    Description = "Review your tool specification",
                    Question = "Does this look correct? (y/n)",
                    Type = StepType.Confirmation,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Choice,
                        AllowedValues = new List<string> { "y", "yes", "n", "no" }
                    }
                }
            };
        }

        private void StoreStepInput(GenerationSession session, GenerationStep step, string input)
        {
            switch (step.Order)
            {
                case 1: // Tool Name
                    session.ToolName = input;
                    break;
                case 2: // Category
                    session.Category = input;
                    break;
                case 3: // Description
                    session.Description = input;
                    break;
                case 4: // Inputs
                    session.Inputs = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .Where(i => !string.IsNullOrEmpty(i))
                                        .ToList();
                    break;
                case 5: // Outputs
                    session.Outputs = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(o => o.Trim())
                                         .Where(o => !string.IsNullOrEmpty(o))
                                         .ToList();
                    break;
                case 6: // Requirements
                    session.Requirements = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(r => r.Trim())
                                              .Where(r => !string.IsNullOrEmpty(r))
                                              .ToList();
                    break;
            }
        }

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

        #endregion
    }
}
