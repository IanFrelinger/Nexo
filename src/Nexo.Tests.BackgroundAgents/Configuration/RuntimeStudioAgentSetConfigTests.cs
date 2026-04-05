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

    [Fact]
    public async Task RuntimeStudioGameDirectorAgentSet_LoadsExpectedPlannerAndSpecializedWorkers()
    {
        var repoRoot = FindRepoRoot();
        var configPath = Path.Combine(repoRoot, "apps", "runtime-studio", "config", "agent_set.game_director.local.json");
        File.Exists(configPath).Should().BeTrue("runtime-studio game-director config must be present");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var loader = new BackgroundAgentConfigLoader(configuration, new DataSensitivityRegistry(), null);
        var configs = await loader.LoadAsync(default);

        configs.Should().HaveCount(6);
        configs.Select(c => c.Id).Should().Contain([
            "game-director-planner",
            "game-worker-asset-pipeline",
            "game-worker-level-layout",
            "game-worker-systems-designer",
            "game-worker-code-optimizer",
            "game-worker-test-automation"
        ]);

        var planner = configs.Single(c => c.Id == "game-director-planner");
        planner.Role.Should().Be("extender");
        planner.Parameters.Should().ContainKey("Objective");
        planner.Parameters.Should().ContainKey("RepoRoot");
        planner.ExfiltrationPolicy.RequireLocalOnly.Should().BeTrue();

        var assetWorker = configs.Single(c => c.Id == "game-worker-asset-pipeline");
        assetWorker.Role.Should().Be("extender");
        assetWorker.ParentId.Should().Be("game-director-planner");
        assetWorker.Parameters.Should().ContainKey("Objective");

        var layoutWorker = configs.Single(c => c.Id == "game-worker-level-layout");
        layoutWorker.Role.Should().Be("extender");
        layoutWorker.ParentId.Should().Be("game-director-planner");

        var systemsWorker = configs.Single(c => c.Id == "game-worker-systems-designer");
        systemsWorker.Role.Should().Be("extender");
        systemsWorker.ParentId.Should().Be("game-director-planner");

        var optimizer = configs.Single(c => c.Id == "game-worker-code-optimizer");
        optimizer.Role.Should().Be("optimizer");
        optimizer.ParentId.Should().Be("game-director-planner");
        optimizer.Parameters.Should().ContainKey("AnalysisPath");

        var tester = configs.Single(c => c.Id == "game-worker-test-automation");
        tester.Role.Should().Be("tester");
        tester.ParentId.Should().Be("game-director-planner");
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
