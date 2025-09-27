// This file has been refactored into specialized test classes and mocks:
// - ToolGeneration/Success/SuccessCaseTests.cs - E2E success flows
// - ToolGeneration/ErrorHandling/ErrorHandlingTests.cs - Error paths
// - ToolGeneration/Cancellation/CancellationTests.cs - Cancellation behavior
// - ToolGeneration/Mocks/MockServices.cs - Test doubles for services

// Re-export for backward compatibility
using Nexo.Infrastructure.Tests.ToolGeneration.Success;
using Nexo.Infrastructure.Tests.ToolGeneration.ErrorHandling;
using Nexo.Infrastructure.Tests.ToolGeneration.Cancellation;
using Nexo.Infrastructure.Tests.ToolGeneration.Mocks;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    // Tests have moved to specialized classes under the namespaces referenced above.
}

        [Fact]
        public async Task ToolEvolutionPipeline_ExistingTool_EvolutionSucceeds()
        {
            // Arrange
            var toolRepo = _host.Services.GetRequiredService<IToolRepository>();
            var evolver = _host.Services.GetRequiredService<IToolEvolver>();
            
            // First create a tool
            var plugin = new MockPlugin("TestPlugin", "1.0", "Test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public class TestPlugin : IPlugin { /* implementation */ }";
            
            await toolRepo.SaveToolAsync(plugin, assembly, sourceCode);

            // Act - Evolve the tool
            var evolutionResult = await evolver.EvolveToolAsync("TestPlugin", "add XML support");

            // Assert
            Assert.NotNull(evolutionResult);
            Assert.Equal("TestPlugin", evolutionResult.Name);
            Assert.Equal(2, evolutionResult.Version);
            Assert.True(evolutionResult.Success);
        }

        [Fact]
        public async Task ToolRepository_CRUDOperations_WorkCorrectly()
        {
            // Arrange
            var toolRepo = _host.Services.GetRequiredService<IToolRepository>();
            var plugin = new MockPlugin("CRUDTest", "1.0", "CRUD test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public class CRUDTest : IPlugin { /* implementation */ }";

            // Act & Assert - Create
            var savedTool = await toolRepo.SaveToolAsync(plugin, assembly, sourceCode);
            Assert.NotNull(savedTool);
            Assert.Equal("CRUDTest", savedTool.Name);

            // Read
            var retrievedTool = await toolRepo.GetToolAsync("CRUDTest");
            Assert.NotNull(retrievedTool);
            Assert.Equal("CRUDTest", retrievedTool.Name);

            // List
            var allTools = await toolRepo.ListToolsAsync();
            Assert.Contains(allTools, t => t.Name == "CRUDTest");

            // Load source
            var loadedSource = await toolRepo.LoadToolSourceAsync("CRUDTest");
            Assert.Equal(sourceCode, loadedSource);

            // Update version
            var newSourceCode = sourceCode + " // Updated";
            await toolRepo.SaveVersionAsync("CRUDTest", newSourceCode, 2);
            
            var updatedSource = await toolRepo.LoadToolSourceAsync("CRUDTest");
            Assert.Equal(newSourceCode, updatedSource);
        }

        [Fact]
        public async Task MultipleTools_CanBeGeneratedAndManaged()
        {
            // Arrange
            var orchestrator = _host.Services.GetRequiredService<ToolGenerationOrchestrator>();
            var toolRepo = _host.Services.GetRequiredService<IToolRepository>();

            // Act - Generate multiple tools
            var tool1 = await orchestrator.GenerateToolAsync("Create a JSON formatter");
            var tool2 = await orchestrator.GenerateToolAsync("Create a CSV parser");
            var tool3 = await orchestrator.GenerateToolAsync("Create a text file reader");

            // Assert
            Assert.NotNull(tool1);
            Assert.NotNull(tool2);
            Assert.NotNull(tool3);

            // Verify all tools are listed
            var allTools = await toolRepo.ListToolsAsync();
            Assert.True(allTools.Count() >= 3);
        }

        public void Dispose()
        {
            _host?.Dispose();
            
            // Clean up test directory
            if (Directory.Exists(_testToolsPath))
            {
                Directory.Delete(_testToolsPath, true);
            }
        }

        // Mock implementations for integration testing
        private class MockCodeGenerator : ICodeGenerator
        {
            public async Task<GeneratedCode> GenerateFromDescriptionAsync(string description, CancellationToken cancellationToken = default)
            {
                await Task.Delay(100, cancellationToken); // Simulate AI processing time
                
                var toolName = ExtractToolName(description);
                return new GeneratedCode
                {
                    SourceCode = GenerateMockCode(toolName),
                    ToolName = toolName,
                    Description = $"Mock {toolName.ToLower()} tool",
                    IsWrappedInPlugin = true
                };
            }

            public async Task<GeneratedCode> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default)
            {
                await Task.Delay(100, cancellationToken);
                
                var toolName = ExtractToolName(prompt);
                return new GeneratedCode
                {
                    SourceCode = GenerateMockCode(toolName),
                    ToolName = toolName,
                    Description = $"Mock {toolName.ToLower()} tool",
                    IsWrappedInPlugin = true
                };
            }

            private static string ExtractToolName(string description)
            {
                if (description.Contains("calculator", StringComparison.OrdinalIgnoreCase))
                    return "Calculator";
                if (description.Contains("JSON", StringComparison.OrdinalIgnoreCase))
                    return "JsonFormatter";
                if (description.Contains("CSV", StringComparison.OrdinalIgnoreCase))
                    return "CsvParser";
                if (description.Contains("text", StringComparison.OrdinalIgnoreCase))
                    return "TextReader";
                
                return "MockTool";
            }

            private static string GenerateMockCode(string toolName)
            {
                return $@"
using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;

public class {toolName} : IPlugin
{{
    public string Name => ""{toolName}"";
    public string Version => ""1.0"";
    public string Description => ""Mock {toolName.ToLower()} tool"";
    public string Author => ""Mock"";
    public bool IsEnabled => true;

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {{
        return Task.CompletedTask;
    }}

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {{
        return Task.CompletedTask;
    }}

    public Task<PluginResult> ExecuteAsync(string[] args)
    {{
        var result = new PluginResult
        {{
            Success = true,
            Message = ""{toolName} executed with args: "" + string.Join("", "", args)
        }};

        // Mock calculator logic
        if (Name == ""Calculator"" && args.Length >= 3)
        {{
            if (args[0] == ""add"" && int.TryParse(args[1], out var a) && int.TryParse(args[2], out var b))
            {{
                result.Message = $""Result: {{a + b}}"";
            }}
        }}

        return Task.FromResult(result);
    }}
}}";
            }
        }

        private class MockCompilationService : ICompilationService
        {
            public async Task<CompilationResult> CompileToAssemblyAsync(string code, CancellationToken cancellationToken = default)
            {
                await Task.Delay(50, cancellationToken); // Simulate compilation time
                
                return new CompilationResult
                {
                    Success = true,
                    Assembly = System.Text.Encoding.UTF8.GetBytes("mock assembly bytes"),
                    CompiledAt = DateTime.UtcNow
                };
            }

            public async Task<CompilationResult> FixAndCompileAsync(string code, string[] errors, CancellationToken cancellationToken = default)
            {
                await Task.Delay(100, cancellationToken);
                
                return new CompilationResult
                {
                    Success = true,
                    Assembly = System.Text.Encoding.UTF8.GetBytes("fixed assembly bytes"),
                    FixedSourceCode = code + " // Fixed by AI",
                    CompiledAt = DateTime.UtcNow
                };
            }
        }

        private class MockToolRepository : IToolRepository
        {
            private readonly string _toolsPath;

            public MockToolRepository()
            {
                _toolsPath = Path.Combine(Path.GetTempPath(), "nexo_mock_tools", Guid.NewGuid().ToString());
                Directory.CreateDirectory(_toolsPath);
            }

            public async Task<SavedTool> SaveToolAsync(IPlugin plugin, byte[] assembly, string sourceCode, CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);
                
                var toolId = Guid.NewGuid();
                var toolDir = Path.Combine(_toolsPath, toolId.ToString());
                Directory.CreateDirectory(toolDir);

                var assemblyPath = Path.Combine(toolDir, $"{plugin.Name}.dll");
                var sourcePath = Path.Combine(toolDir, $"{plugin.Name}.cs");

                await File.WriteAllBytesAsync(assemblyPath, assembly, cancellationToken);
                await File.WriteAllTextAsync(sourcePath, sourceCode, cancellationToken);

                return new SavedTool
                {
                    Id = toolId,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    AssemblyPath = assemblyPath,
                    SourcePath = sourcePath
                };
            }

            public async Task<IEnumerable<ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);
                
                var tools = new List<ToolInfo>();
                foreach (var toolDir in Directory.GetDirectories(_toolsPath))
                {
                    var toolName = Path.GetFileName(toolDir);
                    tools.Add(new ToolInfo
                    {
                        Name = toolName,
                        Description = $"Mock {toolName} tool",
                        Version = 1,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        CommandName = toolName.ToLower(),
                        IsLoaded = false
                    });
                }
                return tools;
            }

            public async Task<ToolInfo?> GetToolAsync(string toolName, CancellationToken cancellationToken = default)
            {
                var tools = await ListToolsAsync(cancellationToken);
                return tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
            }

            public async Task<string?> LoadToolSourceAsync(string toolName, CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);
                
                var toolDirs = Directory.GetDirectories(_toolsPath);
                foreach (var toolDir in toolDirs)
                {
                    if (Path.GetFileName(toolDir).Equals(toolName, StringComparison.OrdinalIgnoreCase))
                    {
                        var sourcePath = Path.Combine(toolDir, $"{toolName}.cs");
                        if (File.Exists(sourcePath))
                        {
                            return await File.ReadAllTextAsync(sourcePath, cancellationToken);
                        }
                    }
                }
                return null;
            }

            public async Task SaveVersionAsync(string toolName, string sourceCode, int version, CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);
                
                var toolDirs = Directory.GetDirectories(_toolsPath);
                foreach (var toolDir in toolDirs)
                {
                    if (Path.GetFileName(toolDir).Equals(toolName, StringComparison.OrdinalIgnoreCase))
                    {
                        var sourcePath = Path.Combine(toolDir, $"{toolName}.cs");
                        await File.WriteAllTextAsync(sourcePath, sourceCode, cancellationToken);
                        break;
                    }
                }
            }
        }

        private class MockToolEvolver : IToolEvolver
        {
            private readonly IToolRepository _toolRepo;
            private readonly ICodeGenerator _codeGenerator;
            private readonly ICompilationService _compiler;
            private readonly IPluginLoader _pluginLoader;

            public MockToolEvolver(IToolRepository toolRepo, ICodeGenerator codeGenerator, ICompilationService compiler, IPluginLoader pluginLoader)
            {
                _toolRepo = toolRepo;
                _codeGenerator = codeGenerator;
                _compiler = compiler;
                _pluginLoader = pluginLoader;
            }

            public async Task<EvolvedTool> EvolveToolAsync(string toolName, string modification, CancellationToken cancellationToken = default)
            {
                await Task.Delay(100, cancellationToken);
                
                var existingCode = await _toolRepo.LoadToolSourceAsync(toolName, cancellationToken);
                if (string.IsNullOrEmpty(existingCode))
                {
                    return new EvolvedTool
                    {
                        Name = toolName,
                        Success = false,
                        Errors = new[] { "Tool source not found" }
                    };
                }

                var evolvedCode = await _codeGenerator.GenerateFromPromptAsync($"Modify {toolName} to {modification}", cancellationToken);
                var compilationResult = await _compiler.CompileToAssemblyAsync(evolvedCode.SourceCode, cancellationToken);
                
                if (!compilationResult.Success)
                {
                    return new EvolvedTool
                    {
                        Name = toolName,
                        Success = false,
                        Errors = compilationResult.Errors
                    };
                }

                var pluginLoadResult = await _pluginLoader.LoadPluginAsync(compilationResult.Assembly, toolName);
                if (!pluginLoadResult.IsSuccess)
                {
                    return new EvolvedTool
                    {
                        Name = toolName,
                        Success = false,
                        Errors = new[] { "Failed to load evolved plugin" }
                    };
                }

                var currentTool = await _toolRepo.GetToolAsync(toolName, cancellationToken);
                var newVersion = (currentTool?.Version ?? 1) + 1;
                
                await _toolRepo.SaveVersionAsync(toolName, evolvedCode.SourceCode, newVersion, cancellationToken);

                return new EvolvedTool
                {
                    Name = toolName,
                    Version = newVersion,
                    EvolutionDescription = modification,
                    Success = true,
                    EvolvedAt = DateTime.UtcNow
                };
            }

            public async Task<bool> CanEvolveToolAsync(string toolName, CancellationToken cancellationToken = default)
            {
                var tool = await _toolRepo.GetToolAsync(toolName, cancellationToken);
                return tool != null;
            }
        }

        private class MockPluginLoader : IPluginLoader
        {
            public async Task<PluginLoadResult> LoadPluginAsync(byte[] assemblyBytes, string pluginName)
            {
                await Task.Delay(10);
                
                return new PluginLoadResult
                {
                    IsSuccess = true,
                    Plugin = new MockPlugin(pluginName, "1.0", $"Mock {pluginName} plugin"),
                    PluginName = pluginName
                };
            }

            public async Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, string pluginName)
            {
                await Task.Delay(10);
                
                return new PluginLoadResult
                {
                    IsSuccess = true,
                    Plugin = new MockPlugin(pluginName, "1.0", $"Mock {pluginName} plugin"),
                    PluginName = pluginName
                };
            }

            public async Task<bool> UnloadPluginAsync(string pluginName)
            {
                await Task.Delay(10);
                return true;
            }

            public async Task<IPlugin?> GetPluginAsync(string pluginName)
            {
                await Task.Delay(10);
                return new MockPlugin(pluginName, "1.0", $"Mock {pluginName} plugin");
            }

            public async Task<bool> IsPluginLoadedAsync(string pluginName)
            {
                await Task.Delay(10);
                return true;
            }
        }

        private class MockPlugin : IPlugin
        {
            public string Name { get; }
            public string Version { get; }
            public string Description { get; }
            public string Author => "Mock";
            public bool IsEnabled => true;

            public MockPlugin(string name, string version, string description)
            {
                Name = name;
                Version = version;
                Description = description;
            }

            public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task ShutdownAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<PluginResult> ExecuteAsync(string[] args)
            {
                var result = new PluginResult
                {
                    Success = true,
                    Message = $"{Name} executed with args: {string.Join(", ", args)}"
                };

                // Mock calculator logic
                if (Name == "Calculator" && args.Length >= 3)
                {
                    if (args[0] == "add" && int.TryParse(args[1], out var a) && int.TryParse(args[2], out var b))
                    {
                        result.Message = $"Result: {a + b}";
                    }
                }

                return Task.FromResult(result);
            }
        }
    }
}
