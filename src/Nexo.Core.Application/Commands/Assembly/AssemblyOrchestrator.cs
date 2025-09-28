using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Orchestrator for assembly-related commands
    /// </summary>
    public class AssemblyOrchestrator : ICommandOrchestrator<object, object>
    {
        private readonly ICommandOrchestrator<object, object> _commandOrchestrator;

        public AssemblyOrchestrator(ICommandOrchestrator<object, object> commandOrchestrator)
        {
            _commandOrchestrator = commandOrchestrator ?? throw new ArgumentNullException(nameof(commandOrchestrator));
        }

        public async Task<object> ExecuteAsync(
            IEnumerable<ICommand<object, object>> commands,
            object input,
            CancellationToken cancellationToken = default)
        {
            return await _commandOrchestrator.ExecuteAsync(commands, input, cancellationToken);
        }

        public async Task<object> ExecuteInOrderAsync(
            string[] commandIds,
            IEnumerable<ICommand<object, object>> availableCommands,
            object input,
            CancellationToken cancellationToken = default)
        {
            return await _commandOrchestrator.ExecuteInOrderAsync(commandIds, availableCommands, input, cancellationToken);
        }

        /// <summary>
        /// Execute a comprehensive assembly analysis workflow
        /// </summary>
        public async Task<AssemblyAnalysisWorkflowResult> ExecuteAssemblyAnalysisWorkflowAsync(
            string assemblyPath,
            AssemblyAnalysisOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var commands = new List<ICommand<object, object>>();
                var results = new Dictionary<string, object>();

                // Step 1: Analyze assembly metadata
                if (options.IncludeMetadataAnalysis)
                {
                    var analyzeCommand = new AnalyzeAssemblyCommand();
                    var analyzeInput = new AnalyzeAssemblyInput
                    {
                        AssemblyPath = assemblyPath,
                        IncludePrivateMembers = options.IncludePrivateMembers,
                        IncludeInternalMembers = options.IncludeInternalMembers,
                        IncludeNestedTypes = options.IncludeNestedTypes,
                        IncludeGenerics = options.IncludeGenerics,
                        IncludeResources = options.IncludeResources,
                        IncludeCustomAttributes = options.IncludeCustomAttributes,
                        IncludeDependencies = options.IncludeDependencies
                    };

                    var analyzeResult = await analyzeCommand.ExecuteAsync(analyzeInput, cancellationToken);
                    results["MetadataAnalysis"] = analyzeResult;
                }

                // Step 2: Decompile assembly if requested
                if (options.IncludeDecompilation)
                {
                    var decompileCommand = new DecompileAssemblyCommand();
                    var decompileInput = new DecompileAssemblyInput
                    {
                        AssemblyPath = assemblyPath,
                        Settings = options.DecompilationSettings
                    };

                    var decompileResult = await decompileCommand.ExecuteAsync(decompileInput, cancellationToken);
                    results["Decompilation"] = decompileResult;
                }

                // Step 3: Security scan if requested
                if (options.IncludeSecurityScan)
                {
                    var securityCommand = new ScanAssemblySecurityCommand();
                    var securityInput = new ScanAssemblySecurityInput
                    {
                        AssemblyPath = assemblyPath,
                        ScanOptions = options.SecurityScanOptions
                    };

                    var securityResult = await securityCommand.ExecuteAsync(securityInput, cancellationToken);
                    results["SecurityScan"] = securityResult;
                }

                // Step 4: IL analysis if requested
                if (options.IncludeILAnalysis)
                {
                    var ilCommand = new AnalyzeILCommand();
                    var ilInput = new AnalyzeILInput
                    {
                        AssemblyPath = assemblyPath,
                        MethodName = options.MethodName,
                        AnalysisOptions = options.ILAnalysisOptions
                    };

                    var ilResult = await ilCommand.ExecuteAsync(ilInput, cancellationToken);
                    results["ILAnalysis"] = ilResult;
                }

                return new AssemblyAnalysisWorkflowResult
                {
                    AssemblyPath = assemblyPath,
                    Results = results,
                    Success = true,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new AssemblyAnalysisWorkflowResult
                {
                    AssemblyPath = assemblyPath,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// Options for assembly analysis workflow
    /// </summary>
    public class AssemblyAnalysisOptions
    {
        public bool IncludeMetadataAnalysis { get; set; } = true;
        public bool IncludeDecompilation { get; set; } = false;
        public bool IncludeSecurityScan { get; set; } = true;
        public bool IncludeILAnalysis { get; set; } = false;
        public bool IncludePrivateMembers { get; set; } = false;
        public bool IncludeInternalMembers { get; set; } = false;
        public bool IncludeNestedTypes { get; set; } = true;
        public bool IncludeGenerics { get; set; } = true;
        public bool IncludeResources { get; set; } = true;
        public bool IncludeCustomAttributes { get; set; } = true;
        public bool IncludeDependencies { get; set; } = true;
        public string MethodName { get; set; } = string.Empty;
        public DecompilationSettings DecompilationSettings { get; set; } = new DecompilationSettings();
        public SecurityScanOptions SecurityScanOptions { get; set; } = new SecurityScanOptions();
        public ILAnalysisOptions ILAnalysisOptions { get; set; } = new ILAnalysisOptions();
    }

    /// <summary>
    /// Result of assembly analysis workflow
    /// </summary>
    public class AssemblyAnalysisWorkflowResult
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
