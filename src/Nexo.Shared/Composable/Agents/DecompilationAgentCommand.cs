using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Composable command for decompilation agents
    /// </summary>
    public class DecompilationAgentCommand : BaseAgentComposableCommand
    {
        public DecompilationAgentCommand(
            DecompilationAgent agent,
            AgentTask agentTask,
            AgentContext agentContext,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(agent, agentTask, agentContext, collectionOps, loopOps, stringOps)
        {
        }
        
        public override ICommandIdentifier Id => new CommandIdentifier("DecompilationAgent", "Nexo.Agents", "1.0.0");
        public override ICommandName Name => new CommandName("DecompilationAgent", "Decompilation Agent", "AI Agents");
        public override ICommandDescription Description => new CommandDescription(
            "AI agent for decompiling and analyzing .NET assemblies",
            "This agent can decompile .NET assemblies, analyze IL code, and extract metadata",
            new[] { "ai", "decompilation", "assembly", "agent" },
            new[] { "Decompile assembly", "Analyze IL code", "Extract metadata" });
        public override ICommandStatus Status => CommandStatus.Ready();
        public override ICommandDependencies Dependencies => CommandDependencies.None();
        public override ICommandCapabilities Capabilities => CommandCapabilities.WithCapabilities(
            new Capability("AssemblyDecompilation", "Decompile .NET assemblies to source code", "Analysis", 9),
            new Capability("ILAnalysis", "Analyze Intermediate Language code", "Analysis", 8),
            new Capability("MetadataExtraction", "Extract assembly metadata and information", "Analysis", 7));
        
        public override async Task<ICommandResult> ExecuteWithAgentAsync()
        {
            try
            {
                // Get task parameters
                var assemblyPath = AgentTask.GetParameter<string>("assemblyPath");
                var outputPath = AgentTask.GetParameter<string>("outputPath");
                
                // Validate parameters
                if (await _stringOps.IsNullOrEmptyAsync(assemblyPath))
                {
                    return CreateFailureResult("Assembly path is required for decompilation");
                }
                
                // Execute decompilation
                var result = await Agent.ExecuteAsync(AgentTask);
                
                if (result.IsSuccess)
                {
                    var metadata = new Dictionary<string, object>
                    {
                        ["AssemblyPath"] = assemblyPath,
                        ["OutputPath"] = outputPath,
                        ["DecompilationTimestamp"] = DateTime.UtcNow
                    };
                    return CreateSuccessResult("Decompilation completed successfully", result.Data, metadata);
                }
                else
                {
                    return CreateFailureResult($"Decompilation failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Decompilation failed: {ex.Message}");
            }
        }
        
        public override async Task<ICommandResult> ValidateWithAgentAsync()
        {
            try
            {
                // Check if agent has decompilation capability
                var hasDecompilationCapability = await _collectionOps.AnyAsync(Agent.Capabilities, 
                    async cap => await _stringOps.ContainsAsync(cap.Name, "Decompilation"));
                
                if (!hasDecompilationCapability)
                {
                    return CreateFailureResult("Agent does not have decompilation capability");
                }
                
                // Check task parameters
                var assemblyPath = AgentTask.GetParameter<string>("assemblyPath");
                if (await _stringOps.IsNullOrEmptyAsync(assemblyPath))
                {
                    return CreateFailureResult("Assembly path is required for decompilation");
                }
                
                return CreateSuccessResult("Decompilation validation passed");
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Decompilation validation failed: {ex.Message}");
            }
        }
    }
}
