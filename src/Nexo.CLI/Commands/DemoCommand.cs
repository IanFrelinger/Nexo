using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command handler for demo operations showcasing Universal Testing Agent and Autonomous Development Agent.
/// </summary>
public class DemoCommand
{
    /// <summary>
    /// Creates the demo command group with all subcommands.
    /// </summary>
    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var demoCmd = new Command("demo", "Demo operations for Universal Testing and Autonomous Development");
        
        // Use the new command classes
        demoCmd.AddCommand(new TestCommand());
        demoCmd.AddCommand(new DevCommand());
        
        return demoCmd;
    }
}
