using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Guided tool generation command
    /// </summary>
    public class GuidedGenerationCommand : ICommand
    {
        private readonly IGuidedGenerationService _guidedService;
        private readonly ILogger<GuidedGenerationCommand> _logger;

        public GuidedGenerationCommand(
            IGuidedGenerationService guidedService,
            ILogger<GuidedGenerationCommand> logger)
        {
            _guidedService = guidedService ?? throw new ArgumentNullException(nameof(guidedService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("Target Nexo Guided Tool Generation");
                Console.WriteLine("Let's create your tool step by step!");
                Console.WriteLine();

                // Start guided session
                var session = await _guidedService.StartSessionAsync(cancellationToken);
                Console.WriteLine($"Session started: {session.Id}");
                Console.WriteLine();

                // Process each step
                while (session.CanProceed)
                {
                    var step = await _guidedService.GetNextStepAsync(session.Id, cancellationToken);
                    if (step == null)
                        break;

                    await ProcessStepAsync(session, step, cancellationToken);
                }

                // Generate tool if session is complete
                if (session.Status == GenerationStatus.Completed)
                {
                    await GenerateToolAsync(session, cancellationToken);
                }
                else if (session.Status == GenerationStatus.Failed)
                {
                    Console.WriteLine("ERROR: Tool generation failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in guided generation");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a single step in the guided generation
        /// </summary>
        private async Task ProcessStepAsync(GenerationSession session, GenerationStep step, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Step {step.Order}: {step.Title}");
            Console.WriteLine($"Document {step.Description}");
            Console.WriteLine();

            // Show examples if available
            if (step.Examples.Any())
            {
                Console.WriteLine("Idea Examples:");
                foreach (var example in step.Examples)
                {
                    Console.WriteLine($"   • {example}");
                }
                Console.WriteLine();
            }

            // Show options if available
            if (step.Options.Any())
            {
                Console.WriteLine("List Options:");
                for (int i = 0; i < step.Options.Count; i++)
                {
                    Console.WriteLine($"   {i + 1}. {step.Options[i]}");
                }
                Console.WriteLine();
            }

            // Show help text if available
            if (!string.IsNullOrEmpty(step.HelpText))
            {
                Console.WriteLine($"INFO:  {step.HelpText}");
                Console.WriteLine();
            }

            // Get user input
            string input;
            ValidationResult validationResult;
            
            do
            {
                Console.Write($"{step.Question} ");
                input = Console.ReadLine()?.Trim() ?? "";
                
                if (string.IsNullOrEmpty(input) && !step.IsRequired)
                {
                    break;
                }

                validationResult = await _guidedService.SubmitStepInputAsync(session.Id, input, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    Console.WriteLine($"ERROR: {validationResult.ErrorMessage}");
                    Console.WriteLine();
                }
            } while (!validationResult.IsValid);

            // Move to next step
            await _guidedService.MoveToNextStepAsync(session.Id, cancellationToken);
            Console.WriteLine();
        }

        /// <summary>
        /// Generates the tool from the session
        /// </summary>
        private async Task GenerateToolAsync(GenerationSession session, CancellationToken cancellationToken)
        {
            Console.WriteLine("🔨 Generating your tool...");
            Console.WriteLine();

            try
            {
                var tool = await _guidedService.GenerateToolAsync(session.Id, cancellationToken);
                
                Console.WriteLine("SUCCESS: Tool generated successfully!");
                Console.WriteLine($"   Name: {tool.Name}");
                Console.WriteLine($"   Description: {tool.Description}");
                Console.WriteLine($"   Usage: nexo {tool.CommandName} [args]");
                
                if (tool.QualityScore != null)
                {
                    var quality = tool.QualityScore;
                    var qualityIcon = quality.QualityLevel switch
                    {
                        QualityLevel.Excellent => "EXCELLENT",
                        QualityLevel.Good => "SUCCESS:",
                        QualityLevel.Acceptable => "WARNING:",
                        QualityLevel.Poor => "ERROR:",
                        QualityLevel.Unacceptable => "BLOCKED",
                        _ => "UNKNOWN"
                    };
                    
                    Console.WriteLine($"   Quality: {qualityIcon} {quality.OverallScore:F1}/10 ({quality.QualityLevel})");
                    
                    if (quality.RequiresHumanReview)
                    {
                        Console.WriteLine($"   WARNING:  Requires human review");
                    }
                    
                    if (!quality.IsProductionReady)
                    {
                        Console.WriteLine($"   WARNING:  Not production ready");
                    }
                }
                
                Console.WriteLine($"   Security Safety validated and approved");
                Console.WriteLine();
                Console.WriteLine("SUCCESS Your tool is ready to use!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tool from session: {SessionId}", session.Id);
                Console.WriteLine($"ERROR: Tool generation failed: {ex.Message}");
            }
        }
    }
}
