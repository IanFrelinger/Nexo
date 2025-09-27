using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Orchestration;
using Nexo.Infrastructure.Tests.ToolGeneration.Mocks;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration.Cancellation
{
    public class CancellationTests
    {
        private readonly IHost _host;

        public CancellationTests()
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
        public async Task GenerateToolAsync_Cancellation_ThrowsOperationCanceled()
        {
            var orchestrator = _host.Services.GetRequiredService<ToolGenerationOrchestrator>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => orchestrator.GenerateToolAsync("desc", cts.Token));
        }
    }
}


