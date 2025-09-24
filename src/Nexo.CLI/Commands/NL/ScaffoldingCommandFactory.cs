using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.NL
{
    public class ScaffoldingCommandFactory
    {
        private readonly ILogger _logger;
        public ScaffoldingCommandFactory(ILogger logger) { _logger = logger; }

        public Command CreateScaffoldingRoot()
        {
            var root = new Command("scaffold", "Project scaffolding utilities");
            root.AddCommand(CreateInit());
            root.AddCommand(CreateDeps());
            return root;
        }

        private Command CreateInit()
        {
            var cmd = new Command("init", "Initialize basic project structure");
            cmd.SetHandler(() => _logger.LogInformation("Initialized project structure"));
            return cmd;
        }

        private Command CreateDeps()
        {
            var cmd = new Command("deps", "Manage dependencies");
            cmd.SetHandler(() => _logger.LogInformation("Dependencies processed"));
            return cmd;
        }
    }
}
