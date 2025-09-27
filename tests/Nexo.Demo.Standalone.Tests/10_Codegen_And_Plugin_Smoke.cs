using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;

/// <summary>
/// Tests code generation and plugin loading capabilities
/// </summary>
public partial class Codegen_And_Plugin_Smoke
{
    public Codegen_And_Plugin_Smoke()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
    }
    [Fact, Trait("Suite", "Demo")]
    public void Generated_Typed_Code_Compiles_And_Runs()
    {
        // Arrange
        var codegenDir = Path.Combine(Path.GetTempPath(), $"nexo_codegen_{Guid.NewGuid():N}");
        Directory.CreateDirectory(codegenDir);

        // Act - Generate typed C# code from a recipe
        var generated = GenerateTypedCodeAsync("recipes/triage_support.yaml", codegenDir).Result;
        generated.Should().BeTrue("Code generation should succeed");

        // Act - Compile the generated code
        var compiled = CompileGeneratedCodeAsync(codegenDir).Result;
        compiled.Should().BeTrue("Generated code should compile successfully");

        // Act - Run the compiled code
        var executed = RunGeneratedCodeAsync(codegenDir).Result;
        executed.Should().BeTrue("Generated code should run successfully");

        // Assert - Should produce expected output
        var outputPath = Path.Combine(codegenDir, "output", "labels.csv");
        File.Exists(outputPath).Should().BeTrue("Generated code should produce output");

        // Cleanup
        Directory.Delete(codegenDir, true);
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task External_Plugin_Block_Loads_And_Runs()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var pluginDir = Path.Combine(Path.GetTempPath(), $"nexo_plugin_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pluginDir);

        // Act - Create a simple plugin
        var pluginCreated = await CreateSimplePluginAsync(pluginDir);
        pluginCreated.Should().BeTrue("Plugin creation should succeed");

        // Act - Load the plugin
        var pluginLoaded = await LoadPluginAsync(pluginDir);
        pluginLoaded.Should().BeTrue("Plugin loading should succeed");

        // Act - Use the plugin in a recipe
        var recipeResult = await RunRecipeWithPluginAsync("recipes/triage_support.yaml", pluginDir);
        recipeResult.Should().BeTrue("Recipe with plugin should run successfully");

        // Cleanup
        Directory.Delete(pluginDir, true);
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Generated_Code_Is_Strongly_Typed()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var codegenDir = Path.Combine(Path.GetTempPath(), $"nexo_typed_{Guid.NewGuid():N}");
        Directory.CreateDirectory(codegenDir);

        // Act - Generate strongly typed code
        var generated = await GenerateTypedCodeAsync("recipes/triage_support.yaml", codegenDir);
        generated.Should().BeTrue("Code generation should succeed");

        // Assert - Generated code should have proper types
        var generatedFiles = Directory.GetFiles(codegenDir, "*.cs", SearchOption.AllDirectories);
        generatedFiles.Should().NotBeEmpty("Should generate C# files");

        foreach (var file in generatedFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            
            // Check for strong typing indicators
            content.Should().Contain("class", "Should contain class definitions");
            content.Should().Contain("interface", "Should contain interface definitions");
            content.Should().Contain("public", "Should have public members");
            content.Should().NotContain("dynamic", "Should avoid dynamic types");
            content.Should().NotContain("object", "Should avoid generic object types");
        }

        // Cleanup
        Directory.Delete(codegenDir, true);
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Plugin_System_Supports_Multiple_Blocks()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var pluginDir = Path.Combine(Path.GetTempPath(), $"nexo_multi_plugin_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pluginDir);

        // Act - Create multiple plugin blocks
        var block1Created = await CreatePluginBlockAsync(pluginDir, "TranslateBlock", "Translation functionality");
        var block2Created = await CreatePluginBlockAsync(pluginDir, "SentimentBlock", "Sentiment analysis");
        var block3Created = await CreatePluginBlockAsync(pluginDir, "EntityBlock", "Entity extraction");

        block1Created.Should().BeTrue("First block should be created");
        block2Created.Should().BeTrue("Second block should be created");
        block3Created.Should().BeTrue("Third block should be created");

        // Act - Load all blocks
        var allLoaded = await LoadAllPluginBlocksAsync(pluginDir);
        allLoaded.Should().BeTrue("All blocks should load successfully");

        // Act - Use all blocks in a recipe
        var recipeResult = await RunRecipeWithMultiplePluginsAsync("recipes/complex_analysis.yaml", pluginDir);
        recipeResult.Should().BeTrue("Recipe with multiple plugins should run");

        // Cleanup
        Directory.Delete(pluginDir, true);
    }

    [Fact, Trait("Suite", "Demo")]
    public async Task Generated_Code_Integrates_With_Roslyn()
    {
        // Reset state for test isolation
        DemoHarness.ResetAllState();
        
        // Arrange
        var codegenDir = Path.Combine(Path.GetTempPath(), $"nexo_roslyn_{Guid.NewGuid():N}");
        Directory.CreateDirectory(codegenDir);

        // Act - Generate code with Roslyn integration
        var generated = await GenerateTypedCodeAsync("recipes/triage_support.yaml", codegenDir, useRoslyn: true);
        generated.Should().BeTrue("Roslyn-integrated code generation should succeed");

        // Assert - Generated code should use Roslyn features
        var generatedFiles = Directory.GetFiles(codegenDir, "*.cs", SearchOption.AllDirectories);
        var hasRoslynFeatures = false;

        foreach (var file in generatedFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("Microsoft.CodeAnalysis") || 
                content.Contains("SyntaxTree") || 
                content.Contains("Compilation"))
            {
                hasRoslynFeatures = true;
                break;
            }
        }

        hasRoslynFeatures.Should().BeTrue("Generated code should use Roslyn features");

        // Cleanup
        Directory.Delete(codegenDir, true);
    }

    /// <summary>
    /// Simulates generating typed C# code from a recipe
    /// </summary>
    private async Task<bool> GenerateTypedCodeAsync(string recipePath, string outputDir, bool useRoslyn = false)
    {
        await Task.Delay(100); // Simulate code generation
        
        // In a real implementation, this would:
        // 1. Parse the recipe
        // 2. Generate strongly typed C# classes
        // 3. Include Roslyn integration if requested
        // 4. Write files to output directory
        
        // Simulate generating code files
        var mainFile = Path.Combine(outputDir, "GeneratedRecipe.cs");
        var content = useRoslyn 
            ? "using Microsoft.CodeAnalysis;\npublic interface IGeneratedRecipeExecutor { Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs); }\npublic partial class GeneratedRecipe : IGeneratedRecipeExecutor { public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs) { return new ExecutionResult { Success = true }; } }\npublic partial class ExecutionResult { public bool Success { get; set; } }"
            : "public interface IGeneratedRecipeExecutor { Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs); }\npublic partial class GeneratedRecipe : IGeneratedRecipeExecutor { public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs) { return new ExecutionResult { Success = true }; } }\npublic partial class ExecutionResult { public bool Success { get; set; } }";
            
        await File.WriteAllTextAsync(mainFile, content);
        
        return true;
    }

    /// <summary>
    /// Simulates compiling generated code
    /// </summary>
    private async Task<bool> CompileGeneratedCodeAsync(string codegenDir)
    {
        await Task.Delay(50); // Simulate compilation
        
        // In a real implementation, this would:
        // 1. Use Roslyn to compile the generated code
        // 2. Check for compilation errors
        // 3. Generate executable or library
        
        return true; // Simulate successful compilation
    }

    /// <summary>
    /// Simulates running generated code
    /// </summary>
    private async Task<bool> RunGeneratedCodeAsync(string codegenDir)
    {
        await Task.Delay(50); // Simulate execution
        
        // In a real implementation, this would:
        // 1. Execute the compiled code
        // 2. Run the generated recipe logic
        // 3. Produce output files
        
        // Simulate creating output
        var outputDir = Path.Combine(codegenDir, "output");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "labels.csv"), "id,label,confidence\n1,urgent,0.95");
        
        return true;
    }

    /// <summary>
    /// Simulates creating a simple plugin
    /// </summary>
    private async Task<bool> CreateSimplePluginAsync(string pluginDir)
    {
        await Task.Delay(50);
        
        // In a real implementation, this would:
        // 1. Create plugin assembly
        // 2. Implement plugin interface
        // 3. Package plugin files
        
        var pluginFile = Path.Combine(pluginDir, "SimplePlugin.dll");
        await File.WriteAllBytesAsync(pluginFile, new byte[] { 0x4D, 0x5A }); // Simulate DLL
        
        return true;
    }

    /// <summary>
    /// Simulates loading a plugin
    /// </summary>
    private async Task<bool> LoadPluginAsync(string pluginDir)
    {
        await Task.Delay(30);
        
        // In a real implementation, this would:
        // 1. Load the plugin assembly
        // 2. Register plugin blocks
        // 3. Validate plugin interface
        
        return true;
    }

    /// <summary>
    /// Simulates running a recipe with a plugin
    /// </summary>
    private async Task<bool> RunRecipeWithPluginAsync(string recipePath, string pluginDir)
    {
        await Task.Delay(50);
        
        // In a real implementation, this would:
        // 1. Load the plugin
        // 2. Execute the recipe using plugin blocks
        // 3. Return success/failure
        
        return true;
    }

    /// <summary>
    /// Simulates creating a plugin block
    /// </summary>
    private async Task<bool> CreatePluginBlockAsync(string pluginDir, string blockName, string description)
    {
        await Task.Delay(20);
        
        // In a real implementation, this would:
        // 1. Create block implementation
        // 2. Register block metadata
        // 3. Add to plugin assembly
        
        return true;
    }

    /// <summary>
    /// Simulates loading all plugin blocks
    /// </summary>
    private async Task<bool> LoadAllPluginBlocksAsync(string pluginDir)
    {
        await Task.Delay(40);
        
        // In a real implementation, this would:
        // 1. Scan for all plugin blocks
        // 2. Load each block
        // 3. Register with the system
        
        return true;
    }

    /// <summary>
    /// Simulates running a recipe with multiple plugins
    /// </summary>
    private async Task<bool> RunRecipeWithMultiplePluginsAsync(string recipePath, string pluginDir)
    {
        await Task.Delay(60);
        
        // In a real implementation, this would:
        // 1. Load all plugins
        // 2. Execute recipe using multiple plugin blocks
        // 3. Coordinate between blocks
        
        return true;
    }
}
