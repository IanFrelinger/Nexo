using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Composable command for security analysis agents
    /// </summary>
    public class SecurityAnalysisAgentCommand : BaseAgentComposableCommand
    {
        public SecurityAnalysisAgentCommand(
            SecurityAnalysisAgent agent,
            AgentTask agentTask,
            AgentContext agentContext,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(agent, agentTask, agentContext, collectionOps, loopOps, stringOps)
        {
        }
        
        public override ICommandIdentifier Id => new CommandIdentifier("SecurityAnalysisAgent", "Nexo.Agents", "1.0.0");
        public override ICommandName Name => new CommandName("SecurityAnalysisAgent", "Security Analysis Agent", "AI Agents");
        public override ICommandDescription Description => new CommandDescription(
            "AI agent for analyzing security vulnerabilities in code and systems",
            "This agent can scan code for security issues, analyze dependencies, and provide security recommendations",
            new[] { "ai", "security", "analysis", "agent" },
            new[] { "Scan for vulnerabilities", "Analyze dependencies", "Security audit" });
        public override ICommandStatus Status => CommandStatus.Ready();
        public override ICommandDependencies Dependencies => CommandDependencies.None();
        public override ICommandCapabilities Capabilities => CommandCapabilities.WithCapabilities(
            new Capability("SecurityAnalysis", "Analyze code for security vulnerabilities", "Security", 9),
            new Capability("VulnerabilityScanning", "Scan for known security vulnerabilities", "Security", 8),
            new Capability("DependencyAnalysis", "Analyze dependencies for security issues", "Security", 7));
        
        public override async Task<ICommandResult> ExecuteWithAgentAsync()
        {
            try
            {
                // Get task parameters
                var targetPath = AgentTask.GetParameter<string>("targetPath");
                var analysisType = AgentTask.GetParameter<string>("analysisType");
                
                // Validate parameters
                if (await _stringOps.IsNullOrEmptyAsync(targetPath))
                {
                    return CreateFailureResult("Target path is required for security analysis");
                }
                
                // Execute security analysis
                var result = await Agent.ExecuteAsync(AgentTask);
                
                if (result.IsSuccess)
                {
                    var metadata = new Dictionary<string, object>
                    {
                        ["TargetPath"] = targetPath,
                        ["AnalysisType"] = analysisType,
                        ["AnalysisTimestamp"] = DateTime.UtcNow
                    };
                    return CreateSuccessResult("Security analysis completed successfully", result.Data, metadata);
                }
                else
                {
                    return CreateFailureResult($"Security analysis failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Security analysis failed: {ex.Message}");
            }
        }
        
        public override async Task<ICommandResult> ValidateWithAgentAsync()
        {
            try
            {
                // Check if agent has security analysis capability
                var hasSecurityCapability = await _collectionOps.AnyAsync(Agent.Capabilities, 
                    async cap => await _stringOps.ContainsAsync(cap.Name, "Security"));
                
                if (!hasSecurityCapability)
                {
                    return CreateFailureResult("Agent does not have security analysis capability");
                }
                
                // Check task parameters
                var targetPath = AgentTask.GetParameter<string>("targetPath");
                if (await _stringOps.IsNullOrEmptyAsync(targetPath))
                {
                    return CreateFailureResult("Target path is required for security analysis");
                }
                
                return CreateSuccessResult("Security analysis validation passed");
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Security analysis validation failed: {ex.Message}");
            }
        }
    }
}
