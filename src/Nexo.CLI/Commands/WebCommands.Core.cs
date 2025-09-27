using System;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Web.Interfaces;
using Nexo.Feature.Web.UseCases;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Core web command orchestration functionality
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
    }
}
