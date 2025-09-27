using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Nexo.CLI.Commands
{
    public partial class CentralCommandAggregator
    {
        private Command CreateOrchestrationCommand()
        {
            var orchestrationCommand = new Command("orchestrate", "Orchestrate multiple commands in sequence");

            var sequenceCommand = new Command("sequence", "Execute a sequence of commands");
            var commandsArgument = new Argument<string[]>("commands", "Commands to execute in sequence");
            sequenceCommand.AddArgument(commandsArgument);

            sequenceCommand.SetHandler(async (string[] commands) =>
            {
                await ExecuteCommandSequence(commands);
            }, commandsArgument);

            orchestrationCommand.AddCommand(sequenceCommand);

            var workflowCommand = new Command("workflow", "Execute predefined workflows");
            var workflowArgument = new Argument<string>("workflow", "Workflow name to execute");
            workflowCommand.AddArgument(workflowArgument);

            workflowCommand.SetHandler(async (string workflow) =>
            {
                await ExecuteWorkflow(workflow);
            }, workflowArgument);

            orchestrationCommand.AddCommand(workflowCommand);

            return orchestrationCommand;
        }

        private async Task ExecuteCommandSequence(string[] commands)
        {
            _logger.LogInformation("Executing command sequence with {Count} commands", commands.Length);

            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                _logger.LogInformation("Executing command {Index}/{Total}: {Command}", i + 1, commands.Length, command);

                try
                {
                    await ExecuteCommand(command, false, true, 300);
                    _logger.LogInformation("Command {Index} completed successfully", i + 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command {Index} failed: {Command}", i + 1, command);
                    throw;
                }
            }

            _logger.LogInformation("All commands in sequence completed successfully");
        }

        private async Task ExecuteWorkflow(string workflowName)
        {
            var workflows = GetPredefinedWorkflows();
            
            if (!workflows.TryGetValue(workflowName, out var workflow))
            {
                _logger.LogError("Workflow '{Workflow}' not found", workflowName);
                Console.WriteLine($"❌ Workflow '{workflowName}' not found. Available workflows:");
                foreach (var wf in workflows.Keys)
                {
                    Console.WriteLine($"  • {wf}");
                }
                return;
            }

            _logger.LogInformation("Executing workflow: {Workflow}", workflowName);
            await ExecuteCommandSequence(workflow.Commands);
        }
    }
}

