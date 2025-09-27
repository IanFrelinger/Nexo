using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Core.Pipeline;
using Nexo.Core.Contracts;
using Nexo.Core.Domain.Models.Extensions;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for the AI-native feature factory.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class FeatureFactoryCommands
    {
        /// <summary>
        /// Creates the feature factory command structure.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection</param>
        /// <param name="logger">The logger instance</param>
        /// <returns>The configured command</returns>
        public static Command CreateFeatureFactoryCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var featureFactoryCommand = new Command("feature", "AI-native feature factory commands");
            
            // Generate command
            var generateCommand = CreateGenerateCommand(serviceProvider, logger);
            featureFactoryCommand.AddCommand(generateCommand);
            
            // Analyze command
            var analyzeCommand = CreateAnalyzeCommand(serviceProvider, logger);
            featureFactoryCommand.AddCommand(analyzeCommand);
            
            // Pipeline command
            var pipelineCommand = CreatePipelineCommand(serviceProvider, logger);
            featureFactoryCommand.AddCommand(pipelineCommand);
            
            return featureFactoryCommand;
        }
        // This class acts as an orchestrator for various CLI command functionalities,
        // with specific categories defined in partial classes.
    }
}