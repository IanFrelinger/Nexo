using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Composable command for code generation agents
    /// </summary>
    public class CodeGenerationAgentCommand : BaseAgentComposableCommand
    {
        public CodeGenerationAgentCommand(
            CodeGenerationAgent agent,
            AgentTask agentTask,
            AgentContext agentContext,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(agent, agentTask, agentContext, collectionOps, loopOps, stringOps)
        {
        }
        
        public override ICommandIdentifier Id => new CommandIdentifier("CodeGenerationAgent", "Nexo.Agents", "1.0.0");
        public override ICommandName Name => new CommandName("CodeGenerationAgent", "Code Generation Agent", "AI Agents");
        public override ICommandDescription Description => new CommandDescription(
            "AI agent for generating code across multiple programming languages",
            "This agent can generate code in various programming languages based on natural language prompts",
            new[] { "ai", "code-generation", "agent" },
            new[] { "Generate C# class", "Create Python function", "Build JavaScript module" });
        public override ICommandStatus Status => CommandStatus.Ready();
        public override ICommandDependencies Dependencies => CommandDependencies.None();
        public override ICommandCapabilities Capabilities => CommandCapabilities.WithCapabilities(
            new Capability("CodeGeneration", "Generate code in multiple languages", "AI", 9),
            new Capability("CodeAnalysis", "Analyze and understand code", "AI", 8),
            new Capability("NaturalLanguageProcessing", "Process natural language prompts", "AI", 7));
        
        public override async Task<ICommandResult> ExecuteWithAgentAsync()
        {
            try
            {
                // Get task parameters
                var prompt = AgentTask.GetParameter<string>("prompt");
                var language = AgentTask.GetParameter<string>("language");
                var framework = AgentTask.GetParameter<string>("framework");
                
                // Validate parameters
                if (await _stringOps.IsNullOrEmptyAsync(prompt))
                {
                    return CreateFailureResult("Code generation prompt is required");
                }
                
                // Execute code generation
                var result = await Agent.ExecuteAsync(AgentTask);
                
                if (result.IsSuccess)
                {
                    // Add metadata about the generation
                    var metadata = new Dictionary<string, object>
                    {
                        ["GeneratedLanguage"] = language ?? "C#",
                        ["Framework"] = framework ?? "Unknown",
                        ["PromptLength"] = prompt?.Length ?? 0
                    };
                    
                    return CreateSuccessResult("Code generation completed successfully", result.Data, metadata);
                }
                else
                {
                    return CreateFailureResult($"Code generation failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Code generation failed: {ex.Message}");
            }
        }
        
        public override async Task<ICommandResult> ValidateWithAgentAsync()
        {
            try
            {
                // Check if agent has code generation capability
                var hasCodeGenCapability = await _collectionOps.AnyAsync(Agent.Capabilities, 
                    async cap => await _stringOps.ContainsAsync(cap.Name, "Code Generation"));
                
                if (!hasCodeGenCapability)
                {
                    return CreateFailureResult("Agent does not have code generation capability");
                }
                
                // Check task parameters
                var prompt = AgentTask.GetParameter<string>("prompt");
                if (await _stringOps.IsNullOrEmptyAsync(prompt))
                {
                    return CreateFailureResult("Code generation prompt is required");
                }
                
                return CreateSuccessResult("Code generation validation passed");
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Code generation validation failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Capability implementation
    /// </summary>
    public class Capability : ICapability
    {
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public int Level { get; }
        
        public Capability(string name, string description, string category, int level)
        {
            Name = name;
            Description = description;
            Category = category;
            Level = level;
        }
    }
}
