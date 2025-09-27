using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Infrastructure.Tests.ToolGeneration.Mocks
{
    internal class MockCodeGenerator : ICodeGenerator
    {
        public async Task<GeneratedCode> GenerateFromDescriptionAsync(string description, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return new GeneratedCode
            {
                Name = "Calculator",
                Description = description,
                SourceCode = "public partial class Calculator { public int Add(int a,int b)=>a+b; }"
            };
        }

        public async Task<GeneratedCode> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return new GeneratedCode
            {
                Name = "EvolvedCalculator",
                Description = prompt,
                SourceCode = "public partial class Calculator { public int Add(int a,int b)=>a+b; public int Sub(int a,int b)=>a-b; }"
            };
        }
    }

    internal class MockCompilationService : ICompilationService
    {
        public async Task<CompilationResult> CompileToAssemblyAsync(string sourceCode, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return new CompilationResult
            {
                Success = true,
                Assembly = new byte[] { 1, 2, 3, 4, 5 },
                Errors = Array.Empty<string>()
            };
        }
    }

    internal class MockToolRepository : IToolRepository
    {
        private readonly string _toolsPath;

        public MockToolRepository()
        {
            _toolsPath = Path.Combine(Path.GetTempPath(), "nexo_integration_test_repo");
            Directory.CreateDirectory(_toolsPath);
        }

        public async Task<SavedTool> SaveToolAsync(IPlugin plugin, byte[] assembly, string sourceCode, CancellationToken cancellationToken = default)
        {
            var toolId = Guid.NewGuid().ToString();
            var toolDir = Path.Combine(_toolsPath, toolId);
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

    internal class MockToolEvolver : IToolEvolver
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
                return new EvolvedTool { Name = toolName, Success = false, Errors = new[] { "Tool source not found" } };
            }

            var evolvedCode = await _codeGenerator.GenerateFromPromptAsync($"Modify {toolName} to {modification}", cancellationToken);
            var compilationResult = await _compiler.CompileToAssemblyAsync(evolvedCode.SourceCode, cancellationToken);
            if (!compilationResult.Success)
            {
                return new EvolvedTool { Name = toolName, Success = false, Errors = compilationResult.Errors };
            }

            var pluginLoadResult = await _pluginLoader.LoadPluginAsync(compilationResult.Assembly, toolName);
            if (!pluginLoadResult.IsSuccess)
            {
                return new EvolvedTool { Name = toolName, Success = false, Errors = new[] { "Failed to load evolved plugin" } };
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

    internal class MockPluginLoader : IPluginLoader
    {
        public async Task<PluginLoadResult> LoadPluginAsync(byte[] assemblyBytes, string pluginName)
        {
            await Task.Delay(10);
            return new PluginLoadResult { IsSuccess = true, Plugin = new MockPlugin(pluginName, "1.0", $"Mock {pluginName} plugin"), PluginName = pluginName };
        }

        public async Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, string pluginName)
        {
            await Task.Delay(10);
            return new PluginLoadResult { IsSuccess = true, Plugin = new MockPlugin(pluginName, "1.0", $"Mock {pluginName} plugin"), PluginName = pluginName };
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

    internal class MockPlugin : IPlugin
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

        public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PluginResult> ExecuteAsync(string[] args)
        {
            var result = new PluginResult { Success = true, Message = $"{Name} executed with args: {string.Join(", ", args)}" };
            if (Name == "Calculator" && args.Length >= 3 && args[0] == "add" && int.TryParse(args[1], out var a) && int.TryParse(args[2], out var b))
            {
                result.Message = $"Result: {a + b}";
            }
            return Task.FromResult(result);
        }
    }
}


