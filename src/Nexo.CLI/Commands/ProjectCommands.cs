using System;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;
using Nexo.CLI.Commands.Project.Handlers;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Orchestrator for project commands that delegates to specialized command handlers.
    /// </summary>
    public static class ProjectCommands
    {
        /// <summary>
        /// Creates the project command with all subcommands.
        /// </summary>
        /// <param name="templateService">Template service.</param>
        /// <param name="intelligentTemplateService">Intelligent template service.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The project command.</returns>
        public static Command CreateProjectCommand(
            ITemplateService templateService,
            IIntelligentTemplateService intelligentTemplateService,
            ILogger logger)
        {
            var projectCommand = new Command("project", "Project templates and scaffolding");
            
            // Initialize command handlers
            var initHandler = new ProjectInitHandler(templateService, intelligentTemplateService, logger);
            var scaffoldHandler = new ProjectScaffoldHandler(templateService, intelligentTemplateService, logger);
            var buildHandler = new ProjectBuildHandler(logger);
            var testHandler = new ProjectTestHandler(logger);
            var deployHandler = new ProjectDeployHandler(logger);
            
            // Add subcommands
            projectCommand.AddCommand(initHandler.CreateInitCommand());
            projectCommand.AddCommand(scaffoldHandler.CreateScaffoldCommand());
            projectCommand.AddCommand(buildHandler.CreateBuildCommand());
            projectCommand.AddCommand(testHandler.CreateTestCommand());
            projectCommand.AddCommand(deployHandler.CreateDeployCommand());
            
            return projectCommand;
        }
    }
}
