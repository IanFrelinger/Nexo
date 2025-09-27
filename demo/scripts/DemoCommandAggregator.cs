using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Playground.Console.Commands;
using Playground.Console.Services;

namespace DemoScripts
{
    /// <summary>
    /// Demo command aggregator that composes all demo-related commands
    /// and provides a unified entry point for demo execution
    /// </summary>
    public partial class DemoCommandAggregator
    {
        private readonly CommandComposer _composer;
        private readonly FeatureService _featureService;
        private readonly FrontendGeneratorService _frontendGenerator;

        public DemoCommandAggregator()
        {
            _composer = new CommandComposer();
            _featureService = new FeatureService();
            _frontendGenerator = new FrontendGeneratorService();
        }

        /// <summary>
        /// Creates the main demo command aggregator
        /// </summary>
        public Command CreateDemoCommand()
        {
            var rootCommand = new Command("demo", "Nexo Feature Lab Demo - Interactive composable features showcase");

            // Add all demo command categories
            rootCommand.AddCommand(CreateFeatureLabCommands());
            rootCommand.AddCommand(CreateValidationCommands());
            rootCommand.AddCommand(CreateShowcaseCommands());
            rootCommand.AddCommand(CreateFrontendCommands());
            rootCommand.AddCommand(CreateOrchestrationCommands());
            rootCommand.AddCommand(CreateDiscoveryCommands());

            return rootCommand;
        }
    }

    /// <summary>
    /// Represents a predefined workflow
    /// </summary>
    public class Workflow
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Commands { get; set; }
    }
}
