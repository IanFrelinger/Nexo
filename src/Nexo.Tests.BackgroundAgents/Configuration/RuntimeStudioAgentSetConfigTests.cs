using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Configuration;

public sealed class RuntimeStudioAgentSetConfigTests
{
    [Fact]
    public async Task RuntimeStudioLocalAgentSet_LoadsExpectedPlannerAndWorkers()
    {
        var repoRoot = FindRepoRoot();
        var configPath = Path.Combine(repoRoot, "apps", "runtime-studio", "config", "agent_set.local.json");
        File.Exists(configPath).Should().BeTrue("runtime-studio app config must be present");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var loader = new BackgroundAgentConfigLoader(configuration, new DataSensitivityRegistry(), null);
        var configs = await loader.LoadAsync(default);

        configs.Should().HaveCount(3);
        configs.Select(c => c.Id).Should().Contain(["runtime-planner", "runtime-worker-optimizer", "runtime-worker-tester"]);

        var planner = configs.Single(c => c.Id == "runtime-planner");
        planner.Role.Should().Be("extender");
        planner.ModelProvider.Should().Be("ollama");
        planner.Parameters.Should().ContainKey("RepoRoot");
        planner.ExfiltrationPolicy.RequireLocalOnly.Should().BeTrue();
        planner.ExfiltrationPolicy.BlockExternalLLMs.Should().BeTrue();

        var optimizer = configs.Single(c => c.Id == "runtime-worker-optimizer");
        optimizer.Role.Should().Be("optimizer");
        optimizer.ParentId.Should().Be("runtime-planner");
        optimizer.Parameters.Should().ContainKey("AnalysisPath");

        var tester = configs.Single(c => c.Id == "runtime-worker-tester");
        tester.Role.Should().Be("tester");
        tester.ParentId.Should().Be("runtime-planner");
        tester.ModelProvider.Should().Be("deterministic");
        tester.Parameters.Should().ContainKey("Filter");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Nexo.sln");
            if (File.Exists(sln))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Nexo.sln from test base directory");
    }
}
