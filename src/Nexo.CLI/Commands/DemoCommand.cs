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
        demoCmd.AddCommand(new UniversalTestCommand());
        demoCmd.AddCommand(new AutonomousDevCommand());
        demoCmd.AddCommand(new DemoSelfExtendCommand());

        // Auto-register demo-generated commands if any are present.
        // These are created by `nexo demo self-extend` and are intentionally ignored by git.
        foreach (var type in typeof(DemoCommand).Assembly.GetTypes())
        {
            if (type.IsAbstract) continue;
            if (type.Namespace != "Nexo.CLI.Commands.DemoGenerated") continue;
            if (!typeof(Command).IsAssignableFrom(type)) continue;

            try
            {
                if (Activator.CreateInstance(type) is Command cmd)
                {
                    demoCmd.AddCommand(cmd);
                }
            }
            catch
            {
                // ignore broken demo-generated commands
            }
        }

        return demoCmd;
    }
}
