using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Infrastructure.Export;

/// <summary>
/// Generates code for bricks and workflows.
/// </summary>
public class CodeGenerator : ICodeGenerator
{
    public Task<string> GenerateDeterministicAsync(Brick brick, ExportTarget target, CancellationToken ct = default)
    {
        // Simple stub implementation - in production, use templates or code generation libraries
        var code = target switch
        {
            ExportTarget.CSharp => GenerateCSharpDeterministic(brick),
            ExportTarget.Python => GeneratePythonDeterministic(brick),
            ExportTarget.TypeScript => GenerateTypeScriptDeterministic(brick),
            ExportTarget.BehaviorTree => GenerateBehaviorTreeStub(brick),
            ExportTarget.StateMachine => GenerateStateMachineStub(brick),
            _ => GenerateGenericStub(brick, target)
        };
        
        return Task.FromResult(code);
    }
    
    public Task<string> GenerateDeterministicWithDataAsync(Brick brick, ExportTarget target, CancellationToken ct = default)
    {
        // Similar to GenerateDeterministicAsync but includes references to generated data files
        var code = GenerateDeterministicAsync(brick, target, ct).Result;
        return Task.FromResult(code + "\n// Uses generated data from data files");
    }
    
    public Task<string> GenerateOrchestrationAsync(Workflow workflow, ExportTarget target, CancellationToken ct = default)
    {
        var code = target switch
        {
            ExportTarget.CSharp => GenerateCSharpOrchestration(workflow),
            ExportTarget.Python => GeneratePythonOrchestration(workflow),
            ExportTarget.TypeScript => GenerateTypeScriptOrchestration(workflow),
            ExportTarget.BehaviorTree => GenerateBehaviorTreeOrchestrationStub(workflow),
            ExportTarget.StateMachine => GenerateStateMachineOrchestrationStub(workflow),
            _ => GenerateGenericOrchestrationStub(workflow, target)
        };
        
        return Task.FromResult(code);
    }
    
    public Task<string> GenerateRuntimeBootstrapAsync(Workflow workflow, ExportTarget target, bool includeFallbacks, CancellationToken ct = default)
    {
        var code = $@"// Nexo Runtime Bootstrap
// Workflow: {workflow.Name}
// Include Fallbacks: {includeFallbacks}

using Nexo.Runtime;

var workflow = WorkflowLoader.Load(""workflow.json"");
var executor = new WorkflowExecutor();
await executor.ExecuteAsync(workflow);
";
        
        return Task.FromResult(code);
    }
    
    public Task<ExportedFile> GenerateStaticDataAsync(Brick brick, GeneratedContent content, ExportTarget target, CancellationToken ct = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(content.Variations, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        return Task.FromResult(new ExportedFile
        {
            Path = $"data/{brick.Id}.json",
            Content = json,
            Language = "json"
        });
    }
    
    private string GenerateCSharpDeterministic(Brick brick)
    {
        return $@"// Generated from {brick.Name}
namespace Generated
{{
    public class {brick.Name.Replace(" ", "")}Brick
    {{
        public object Execute(object input)
        {{
            // Deterministic implementation
            return new {{ result = ""stub"" }};
        }}
    }}
}}";
    }
    
    private string GeneratePythonDeterministic(Brick brick)
    {
        return $@"# Generated from {brick.Name}
def execute(input):
    # Deterministic implementation
    return {{'result': 'stub'}}";
    }
    
    private string GenerateTypeScriptDeterministic(Brick brick)
    {
        return $@"// Generated from {brick.Name}
export function execute(input: any): any {{
    // Deterministic implementation
    return {{ result: 'stub' }};
}}";
    }
    
    private string GenerateCSharpOrchestration(Workflow workflow)
    {
        return $@"// Orchestration for {workflow.Name}
namespace Generated
{{
    public class Orchestrator
    {{
        public async Task ExecuteAsync()
        {{
            // Execute {workflow.Instances.Count} instances
        }}
    }}
}}";
    }
    
    private string GeneratePythonOrchestration(Workflow workflow)
    {
        return $@"# Orchestration for {workflow.Name}
async def execute():
    # Execute {workflow.Instances.Count} instances
    pass";
    }
    
    private string GenerateTypeScriptOrchestration(Workflow workflow)
    {
        return $@"// Orchestration for {workflow.Name}
export async function execute(): Promise<void> {{
    // Execute {workflow.Instances.Count} instances
}}";
    }

    private static string GenerateBehaviorTreeStub(Brick brick)
    {
        var safeName = brick.Name.Replace(" ", "");
        return $@"{{
  ""name"": ""{safeName}"",
  ""type"": ""sequence"",
  ""children"": [
    {{ ""type"": ""action"", ""name"": ""{brick.Name}"" }}
  ]
}}";
    }

    private static string GenerateStateMachineStub(Brick brick)
    {
        var safeName = brick.Name.Replace(" ", "");
        return $@"{{
  ""states"": [
    {{ ""id"": ""initial"", ""transitions"": [{{ ""target"": ""{safeName}"" }}] }},
    {{ ""id"": ""{safeName}"", ""transitions"": [] }}
  ]
}}";
    }

    private static string GenerateGenericStub(Brick brick, ExportTarget target)
    {
        return $@"// Generated stub for {brick.Name}
// Target: {target}
// Add implementation for this export target in CodeGenerator
namespace Generated
{{
    public class {brick.Name.Replace(" ", "")}Brick
    {{
        public object Execute(object input) => new {{ result = ""stub"" }};
    }}
}}";
    }

    private static string GenerateBehaviorTreeOrchestrationStub(Workflow workflow)
    {
        return $@"{{
  ""name"": ""{workflow.Name}"",
  ""type"": ""sequence"",
  ""children"": []
}}";
    }

    private static string GenerateStateMachineOrchestrationStub(Workflow workflow)
    {
        return $@"{{
  ""states"": [
    {{ ""id"": ""start"", ""transitions"": [] }}
  ]
}}";
    }

    private static string GenerateGenericOrchestrationStub(Workflow workflow, ExportTarget target)
    {
        return $@"// Orchestration stub for {workflow.Name}
// Target: {target}
// Add implementation for this export target in CodeGenerator";
    }
}

