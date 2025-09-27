using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Orchestration;
using Nexo.Infrastructure.Tests.ToolGeneration.Mocks;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration.ErrorHandling
{
    public partial class ErrorHandlingTests
    {
        private readonly IHost _host;

        public ErrorHandlingTests()
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
        public async Task EvolveToolAsync_NonexistentTool_ReturnsFailure()
        {
            var evolver = _host.Services.GetRequiredService<IToolEvolver>();
            var result = await evolver.EvolveToolAsync("MissingTool", "add feature");
            Assert.False(result.Success);
            Assert.Contains("not found", string.Join(" ", result.Errors ?? Array.Empty<string>()).ToLower());
        }
    }
}


