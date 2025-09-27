using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.UseCases;
using System.Linq;
using System.IO;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Web-related commands for the CLI.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class WebCommands
    {
        /// <summary>
        /// Creates the web command with all its subcommands.
        /// </summary>
        /// <param name="webCodeGenerator">Web code generator service.</param>
        /// <param name="wasmOptimizer">WebAssembly optimizer service.</param>
        /// <param name="generateWebCodeUseCase">Web code generation use case.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Configured web command.</returns>
        public static Command CreateWebCommand(
            IWebCodeGenerator webCodeGenerator,
            IWebAssemblyOptimizer wasmOptimizer,
            GenerateWebCodeUseCase generateWebCodeUseCase,
            ILogger logger)
        {
            var webCommand = new Command("web", "Web code generation and optimization tools");

            // Generate command
            var generateCommand = CreateGenerateCommand(webCodeGenerator, generateWebCodeUseCase, logger);
            webCommand.AddCommand(generateCommand);

            // Optimize command
            var optimizeCommand = CreateOptimizeCommand(wasmOptimizer, logger);
            webCommand.AddCommand(optimizeCommand);

            // Analyze command
            var analyzeCommand = CreateAnalyzeCommand(wasmOptimizer, logger);
            webCommand.AddCommand(analyzeCommand);

            // List command
            var listCommand = CreateListCommand(webCodeGenerator, wasmOptimizer, logger);
            webCommand.AddCommand(listCommand);

            // Validate command
            var validateCommand = CreateValidateCommand(webCodeGenerator, wasmOptimizer, logger);
            webCommand.AddCommand(validateCommand);

            return webCommand;
        }
        // This class acts as an orchestrator for various web command functionalities,
        // with specific categories defined in partial classes.
    }
}
