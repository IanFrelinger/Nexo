using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Orchestration;
using Nexo.Infrastructure.Tests.ToolGeneration.Mocks;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration.Success
{
    public class SuccessCaseTests
    {
        private readonly IHost _host;

        public SuccessCaseTests()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
                    services.AddTransient<ICodeGenerator, MockCodeGenerator>();
                    services.AddTransient<ICompilationService, MockCompilationService>();
                    services.AddTransient<IToolRepository, MockToolRepository>();
                    services.AddTransient<IToolEvolver, MockToolEvolver>();
                    services.AddTransient<IPluginLoader, MockPluginLoader>();
                    services.AddTransient<ToolGenerationOrchestrator>();
                })
                .Build();
        }

        [Fact]
        public async Task CompleteToolGenerationPipeline_ValidDescription_GeneratesWorkingTool()
        {
            var orchestrator = _host.Services.GetRequiredService<ToolGenerationOrchestrator>();
            var description = "Create a simple calculator that can add and subtract numbers";
            var result = await orchestrator.GenerateToolAsync(description);
            Assert.NotNull(result);
            Assert.Equal("Calculator", result.Name);
            Assert.Contains("calculator", result.Description.ToLower());
            Assert.True(result.IsReady);
            Assert.NotNull(result.Plugin);
            var executionResult = await result.ExecuteAsync(new[] { "add", "5", "3" });
            Assert.True(executionResult.Success);
            Assert.Contains("8", executionResult.Message);
        }
    }
}


