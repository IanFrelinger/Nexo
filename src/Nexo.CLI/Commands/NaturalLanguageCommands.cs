using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Commands.NL;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Orchestrator for natural-language driven CLI commands. Delegates to focused factories.
    /// </summary>
    public partial class NaturalLanguageCommands
    {
        private readonly ILogger<NaturalLanguageCommands> _logger;
        private readonly BuildCommandFactory _buildFactory;
        private readonly QuickCommandsFactory _quickFactory;
        private readonly TemplatesCommandFactory _templatesFactory;
        private readonly ScaffoldingCommandFactory _scaffoldingFactory;

        public NaturalLanguageCommands(ILogger<NaturalLanguageCommands> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _buildFactory = new BuildCommandFactory(_logger);
            _quickFactory = new QuickCommandsFactory(_logger);
            _templatesFactory = new TemplatesCommandFactory(_logger);
            _scaffoldingFactory = new ScaffoldingCommandFactory(_logger);
        }

        public Command CreateNaturalLanguageCommand()
        {
            var root = new Command("build", "Build applications using natural language descriptions");

            // Main build command
            root.AddCommand(_buildFactory.CreateBuildCommand());

            // Quick subcommands
            root.AddCommand(_quickFactory.CreateQuickRoot());

            // Templates listing
            root.AddCommand(_templatesFactory.CreateTemplatesCommand());

            // Scaffolding utilities
            root.AddCommand(_scaffoldingFactory.CreateScaffoldingRoot());

            return root;
        }
    }
}
