using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using System.Linq;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Development-related commands for the CLI.
    /// </summary>
    public static class DevelopmentCommands
    {
        /// <summary>
        /// Creates the development command with all its subcommands.
        /// </summary>
        /// <param name="developmentAccelerator">Development accelerator service.</param>
        /// <param name="workflowExecutionService">Workflow execution service.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Configured development command.</returns>
        public static Command CreateDevelopmentCommand(
            IDevelopmentAccelerator developmentAccelerator,
            IWorkflowExecutionService workflowExecutionService,
            ILogger logger)
        {
            var devCommand = new Command("dev", "Development tools and utilities");

            // Test generation command
            var testCommand = new Command("test", "Test generation and management");
            var testGenerateOption = new Option<string>("--generate", "File or code to generate tests for") { IsRequired = false };
            var testFrameworkOption = new Option<string>("--framework", "Test framework to use") { IsRequired = false };
            var testOutputOption = new Option<string>("--output", "Output file for generated tests") { IsRequired = false };

            testCommand.AddOption(testGenerateOption);
            testCommand.AddOption(testFrameworkOption);
            testCommand.AddOption(testOutputOption);

            testCommand.SetHandler(async (generate, framework, output) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(generate))
                    {
                        logger.LogInformation("Generating tests for: {File}", generate);
                        
                        var context = !string.IsNullOrEmpty(framework) 
                            ? new Dictionary<string, object> { { "framework", framework } }
                            : null;
                        
                        var tests = await developmentAccelerator.GenerateTestsAsync(generate, context);
                        
                        if (!string.IsNullOrEmpty(output))
                        {
#if NET8_0_OR_GREATER
                            await System.IO.File.WriteAllLinesAsync(output, tests);
#else
                            System.IO.File.WriteAllLines(output, tests);
#endif
                            Console.WriteLine($"Generated {tests.Count} test methods and saved to {output}");
                        }
                        else
                        {
                            foreach (var test in tests)
                            {
                                Console.WriteLine("---");
                                Console.WriteLine(test);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Please specify --generate with a file or code to generate tests for");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to generate tests");
                    Console.WriteLine($"Error: Failed to generate tests: {ex.Message}");
                }
            }, testGenerateOption, testFrameworkOption, testOutputOption);
            
            devCommand.AddCommand(testCommand);
            
            // Workflow command
            var workflowCommand = new Command("workflow", "Development workflow automation");
            var workflowTypeOption = new Option<string>("--type", "Workflow type (setup, analyze, test, deploy)") { IsRequired = true };
            var workflowProjectOption = new Option<string>("--project", "Project path") { IsRequired = false };
            var workflowConfigOption = new Option<string>("--config", "Workflow configuration file") { IsRequired = false };
            
            workflowCommand.AddOption(workflowTypeOption);
            workflowCommand.AddOption(workflowProjectOption);
            workflowCommand.AddOption(workflowConfigOption);
            
            workflowCommand.SetHandler(async (type, project, config) =>
            {
                try
                {
                    logger.LogInformation("Executing workflow: {Type}", type);
                    
                    // Parse workflow type
                    if (!Enum.TryParse<WorkflowType>(type, true, out var workflowType))
                    {
                        Console.WriteLine($"Error: Invalid workflow type '{type}'. Valid types are: setup, analyze, test, deploy");
                        return;
                    }

                    // Determine project path
                    var projectPath = project ?? Environment.CurrentDirectory;
                    
                    Console.WriteLine($"Executing {workflowType} workflow for project: {projectPath}");
                    
                    // Execute workflow using the workflow execution service
                    var result = await workflowExecutionService.ExecuteWorkflowAsync(
                        workflowType, 
                        projectPath, 
                        config, 
                        CancellationToken.None);

                    // Display results
                    Console.WriteLine($"\nWorkflow execution completed:");
                    Console.WriteLine($"  Status: {result.Status}");
                    Console.WriteLine($"  Duration: {result.Duration?.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"  Total Steps: {result.TotalSteps}");
                    Console.WriteLine($"  Successful: {result.SuccessfulSteps}");
                    Console.WriteLine($"  Failed: {result.FailedSteps}");
                    Console.WriteLine($"  Skipped: {result.SkippedSteps}");

                    if (!result.IsSuccess)
                    {
                        Console.WriteLine($"  Error: {result.ErrorMessage}");
                    }

                    // Display step results
                    if (result.StepResults.Any())
                    {
                        Console.WriteLine("\nStep Results:");
                        foreach (var stepResult in result.StepResults)
                        {
                            var statusIcon = stepResult.IsSuccess ? "✓" : stepResult.Status == WorkflowStepStatus.Skipped ? "⏭" : "✗";
                            Console.WriteLine($"  {statusIcon} {stepResult.Step.Name} ({stepResult.Duration.TotalMilliseconds:F0}ms)");
                            
                            if (!stepResult.IsSuccess && !string.IsNullOrEmpty(stepResult.ErrorMessage))
                            {
                                Console.WriteLine($"    Error: {stepResult.ErrorMessage}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to execute workflow {Type}", type);
                    Console.WriteLine($"Error: Failed to execute workflow: {ex.Message}");
                }
            }, workflowTypeOption, workflowProjectOption, workflowConfigOption);
            
            devCommand.AddCommand(workflowCommand);
            
            return devCommand;
        }
    }
} 