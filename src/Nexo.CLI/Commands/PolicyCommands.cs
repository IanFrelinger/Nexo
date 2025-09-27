using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// CLI commands for policy management and validation
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class PolicyCommands : ICommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PolicyCommands> _logger;

        public PolicyCommands(IServiceProvider serviceProvider, ILogger<PolicyCommands> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateCommand()
        {
            var policyCommand = new Command("policy", "Policy management and validation commands");

            // Safety scan command
            var safetyScanCommand = new Command("safety", "Run safety policy scan");
            var safetyPolicyArg = new Argument<string>("policy", "Path to safety policy file");
            var safetyOutputArg = new Argument<string>("output", "Output directory for safety report");
            var safetyFormatArg = new Option<string[]>("--format", "Output formats (json, md, sarif)") { AllowMultipleArgumentsPerToken = true };

            safetyScanCommand.AddArgument(safetyPolicyArg);
            safetyScanCommand.AddArgument(safetyOutputArg);
            safetyScanCommand.AddOption(safetyFormatArg);

            safetyScanCommand.SetHandler(async (policyPath, outputPath, formats) =>
            {
                await RunSafetyScanAsync(policyPath, outputPath, formats);
            }, safetyPolicyArg, safetyOutputArg, safetyFormatArg);

            // Quality run command
            var qualityRunCommand = new Command("quality", "Run quality policy validation");
            var qualityPolicyArg = new Argument<string>("policy", "Path to quality policy file");
            var qualityOutputArg = new Argument<string>("output", "Output directory for quality report");
            var qualityFormatArg = new Option<string[]>("--format", "Output formats (json, md, sarif)") { AllowMultipleArgumentsPerToken = true };

            qualityRunCommand.AddArgument(qualityPolicyArg);
            qualityRunCommand.AddArgument(qualityOutputArg);
            qualityRunCommand.AddOption(qualityFormatArg);

            qualityRunCommand.SetHandler(async (policyPath, outputPath, formats) =>
            {
                await RunQualityValidationAsync(policyPath, outputPath, formats);
            }, qualityPolicyArg, qualityOutputArg, qualityFormatArg);

            // Policy apply command
            var policyApplyCommand = new Command("apply", "Apply policy manifest");
            var manifestArg = new Argument<string>("manifest", "Path to policy manifest file");
            var policyOutputArg = new Argument<string>("output", "Output directory for policy report");
            var policyCodeArg = new Option<string>("--code", "Code to validate (if not provided, reads from stdin)");

            policyApplyCommand.AddArgument(manifestArg);
            policyApplyCommand.AddArgument(policyOutputArg);
            policyApplyCommand.AddOption(policyCodeArg);

            policyApplyCommand.SetHandler(async (manifestPath, outputPath, code) =>
            {
                await ApplyPolicyManifestAsync(manifestPath, outputPath, code);
            }, manifestArg, policyOutputArg, policyCodeArg);

            // Policy validate command
            var policyValidateCommand = new Command("validate", "Validate policy file against schema");
            var validatePolicyArg = new Argument<string>("policy", "Path to policy file");
            var validateSchemaArg = new Argument<string>("schema", "Path to JSON schema file");

            policyValidateCommand.AddArgument(validatePolicyArg);
            policyValidateCommand.AddArgument(validateSchemaArg);

            policyValidateCommand.SetHandler(async (policyPath, schemaPath) =>
            {
                await ValidatePolicyAsync(policyPath, schemaPath);
            }, validatePolicyArg, validateSchemaArg);

            policyCommand.AddCommand(safetyScanCommand);
            policyCommand.AddCommand(qualityRunCommand);
            policyCommand.AddCommand(policyApplyCommand);
            policyCommand.AddCommand(policyValidateCommand);

            return policyCommand;
        }
        // This class acts as an orchestrator for various policy command functionalities,
        // with specific categories defined in partial classes.
    }
}