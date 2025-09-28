using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Factory for creating AI agent composable commands
    /// </summary>
    public class AgentComposableCommandFactory
    {
        private readonly IDependencyContainer _container;
        
        public AgentComposableCommandFactory(IDependencyContainer? container = null)
        {
            _container = container ?? new DependencyContainer();
        }
        
        /// <summary>
        /// Creates a composable command for a specific agent and task
        /// </summary>
        public async Task<IAgentComposableCommand> CreateAgentCommandAsync(
            IAgent agent,
            AgentTask agentTask,
            AgentContext agentContext)
        {
            // Resolve dependencies
            var collectionOps = await _container.ResolveAsync<ICollectionOperations>();
            var loopOps = await _container.ResolveAsync<ILoopOperations>();
            var stringOps = await _container.ResolveAsync<IStringOperations>();
            
            // Create command based on agent type
            return agent switch
            {
                CodeGenerationAgent codeAgent => new CodeGenerationAgentCommand(
                    codeAgent, agentTask, agentContext, collectionOps, loopOps, stringOps),
                SecurityAnalysisAgent securityAgent => new SecurityAnalysisAgentCommand(
                    securityAgent, agentTask, agentContext, collectionOps, loopOps, stringOps),
                DecompilationAgent decompileAgent => new DecompilationAgentCommand(
                    decompileAgent, agentTask, agentContext, collectionOps, loopOps, stringOps),
                _ => new GenericAgentCommand(
                    agent, agentTask, agentContext, collectionOps, loopOps, stringOps)
            };
        }
        
        /// <summary>
        /// Creates a composable command for code generation
        /// </summary>
        public async Task<CodeGenerationAgentCommand> CreateCodeGenerationCommandAsync(
            CodeGenerationAgent agent,
            string codePrompt,
            string language = "C#",
            string? framework = null)
        {
            var agentTask = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Code Generation"),
                new TaskDescription($"Generate {language} code: {codePrompt}"),
                TaskType.CodeGeneration,
                TaskPriority.High,
                "Code Generation");
            
            // Set task parameters
            agentTask.SetParameter("prompt", codePrompt);
            agentTask.SetParameter("language", language);
            agentTask.SetParameter("framework", framework ?? "default");
            
            var agentContext = new AgentContext(agent.Platform);
            
            var collectionOps = await _container.ResolveAsync<ICollectionOperations>();
            var loopOps = await _container.ResolveAsync<ILoopOperations>();
            var stringOps = await _container.ResolveAsync<IStringOperations>();
            
            return new CodeGenerationAgentCommand(
                agent, agentTask, agentContext, collectionOps, loopOps, stringOps);
        }
        
        /// <summary>
        /// Creates a composable command for security analysis
        /// </summary>
        public async Task<SecurityAnalysisAgentCommand> CreateSecurityAnalysisCommandAsync(
            SecurityAnalysisAgent agent,
            string targetPath,
            string analysisType = "full")
        {
            var agentTask = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Security Analysis"),
                new TaskDescription($"Analyze security of {targetPath}"),
                TaskType.SecurityAnalysis,
                TaskPriority.High,
                "Security Analysis");
            
            // Set task parameters
            agentTask.SetParameter("targetPath", targetPath);
            agentTask.SetParameter("analysisType", analysisType);
            
            var agentContext = new AgentContext(agent.Platform);
            
            var collectionOps = await _container.ResolveAsync<ICollectionOperations>();
            var loopOps = await _container.ResolveAsync<ILoopOperations>();
            var stringOps = await _container.ResolveAsync<IStringOperations>();
            
            return new SecurityAnalysisAgentCommand(
                agent, agentTask, agentContext, collectionOps, loopOps, stringOps);
        }
        
        /// <summary>
        /// Creates a composable command for assembly decompilation
        /// </summary>
        public async Task<DecompilationAgentCommand> CreateDecompilationCommandAsync(
            DecompilationAgent agent,
            string assemblyPath,
            string? outputPath = null)
        {
            var agentTask = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Assembly Decompilation"),
                new TaskDescription($"Decompile assembly {assemblyPath}"),
                TaskType.AssemblyAnalysis,
                TaskPriority.Medium,
                "Assembly Analysis");
            
            // Set task parameters
            agentTask.SetParameter("assemblyPath", assemblyPath);
            agentTask.SetParameter("outputPath", outputPath ?? $"{assemblyPath}.decompiled");
            
            var agentContext = new AgentContext(agent.Platform);
            
            var collectionOps = await _container.ResolveAsync<ICollectionOperations>();
            var loopOps = await _container.ResolveAsync<ILoopOperations>();
            var stringOps = await _container.ResolveAsync<IStringOperations>();
            
            return new DecompilationAgentCommand(
                agent, agentTask, agentContext, collectionOps, loopOps, stringOps);
        }
    }
}
