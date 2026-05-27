using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Assets;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using System.Reflection;
using Xunit;

namespace Nexo.Tests.Orchestration;

public class OrchestrationAgentsGapCoverageTests
{
    [Fact]
    public async Task LifecycleManager_registers_executes_and_shuts_down_agents()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);
        var container = CreateGenericContainer("life-1");

        var registered = await manager.RegisterAgentAsync(container);
        registered.AgentId.Should().Be("life-1");
        manager.GetActiveAgents().Should().ContainSingle();
        manager.GetAgent("life-1").Should().NotBeNull();

        var output = await manager.ExecuteAgentAsync("life-1");
        output.Should().NotBeNull();

        await manager.ShutdownAgentAsync("life-1");
        manager.GetAgent("life-1").Should().BeNull();
        monitor.Dispose();
    }

    [Fact]
    public async Task LifecycleManager_shutdown_all_and_hot_reload_replace_agent()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);

        await manager.RegisterAgentAsync(CreateGenericContainer("a1"));
        await manager.RegisterAgentAsync(CreateGenericContainer("a2"));
        manager.GetActiveAgents().Should().HaveCount(2);

        await manager.ShutdownAllAsync();
        manager.GetActiveAgents().Should().BeEmpty();

        var first = CreateGenericContainer("reload", goal: "first");
        await manager.RegisterAgentAsync(first);
        var reloaded = CreateGenericContainer("reload", goal: "second");
        (await manager.HotReloadAgentAsync(reloaded)).Agent.Spec.Goal.Should().Be("second");
        monitor.Dispose();
    }

    [Fact]
    public async Task LifecycleManager_rejects_invalid_handles_and_missing_agents()
    {
        var manager = new LifecycleManager(
            NullLogger<LifecycleManager>.Instance,
            new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1)));

        var actNull = () => manager.RegisterAgentAsync((IAgentHandle)null!);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        var remoteHandle = Mock.Of<IAgentHandle>();
        var actRemote = () => manager.RegisterAgentAsync(remoteHandle);
        await actRemote.Should().ThrowAsync<InvalidOperationException>();

        var actExecute = () => manager.ExecuteAgentAsync("missing");
        await actExecute.Should().ThrowAsync<InvalidOperationException>();

        await manager.ShutdownAgentAsync("missing");
    }

    [Fact]
    public async Task LifecycleManager_force_terminates_when_shutdown_fails()
    {
        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, monitor);
        var container = new AgentContainer(
            new FailingShutdownAgent(
                new AgentSpawnSpec { AgentId = "bad-shutdown", Domain = "Test", Goal = "x" },
                NullLogger<BaseAgent>.Instance),
            NullLogger<AgentContainer>.Instance);

        await manager.RegisterAgentAsync(container);

        var act = () => manager.ShutdownAgentAsync("bad-shutdown");
        await act.Should().ThrowAsync<InvalidOperationException>();
        manager.GetAgent("bad-shutdown").Should().BeNull();
        monitor.Dispose();
    }

    [Fact]
    public async Task BaseAgent_covers_failure_and_dependency_wait_paths()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "dep-agent",
            Domain = "Test",
            Goal = "wait",
            Dependencies = new[] { "upstream" },
        };
        var agent = new GenericAgent(spec, NullLogger<GenericAgent>.Instance);
        await agent.InitializeAsync();

        await agent.WaitForDependenciesAsync(new Dictionary<string, object>());
        agent.State.Should().Be(AgentState.WaitingForDependencies);

        await agent.WaitForDependenciesAsync(new Dictionary<string, object> { ["upstream"] = "ok" });
        agent.State.Should().Be(AgentState.Ready);

        var actInit = () => agent.InitializeAsync();
        await actInit.Should().ThrowAsync<InvalidOperationException>();

        SetAgentState(agent, AgentState.Created);
        var actExecuteEarly = () => agent.ExecuteAsync();
        await actExecuteEarly.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BaseAgent_execute_failure_and_think_async_paths()
    {
        var failing = new FailingExecuteAgent(
            new AgentSpawnSpec { AgentId = "fail-exec", Domain = "Test", Goal = "boom" },
            NullLogger<BaseAgent>.Instance);
        await failing.InitializeAsync();

        var act = () => failing.ExecuteAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
        failing.State.Should().Be(AgentState.Failed);
        failing.Health.Should().Be(AgentHealth.Unhealthy);

        var ready = new GenericAgent(
            new AgentSpawnSpec { AgentId = "think", Domain = "Test", Goal = "think" },
            NullLogger<GenericAgent>.Instance);
        await ready.InitializeAsync();
        (await ready.ThinkAsync(new AgentObservation(WorldSnapshot.ForRepo(".")), Mock.Of<IToolbox>(), Mock.Of<IAgentMemory>(), CancellationToken.None))
            .Should().Be(AgentActions.None);
        ready.State.Should().Be(AgentState.Completed);
    }

    [Fact]
    public async Task AudioAssetAgent_generates_speech_when_voice_type_requested()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-speech-" + Guid.NewGuid() + ".wav");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            var audioGen = new Mock<IAudioGenerator>();
            audioGen.Setup(g => g.GenerateSpeechAsync(It.IsAny<SpeechGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GeneratedAudio { FilePath = tempFile, MimeType = "audio/wav", DurationMs = 1000 });

            var agent = new AudioAssetAgent(
                new AgentSpawnSpec { AgentId = "voice-1", Domain = "Assets", Goal = "Narration" },
                audioGen.Object,
                model: null,
                Mock.Of<IAssetStorage>(),
                NullLogger<BaseAgent>.Instance);

            var asset = await InvokeGenerateAssetAsync(agent, new GenerationPrompt
            {
                TextPrompt = "Hello world",
                Parameters = new Dictionary<string, object>
                {
                    ["audioType"] = "voice",
                    ["text"] = "Hello world",
                },
            }, CancellationToken.None);

            asset.Should().NotBeNull();
            asset.FilePath.Should().Be(tempFile);
            audioGen.Verify(g => g.GenerateSpeechAsync(It.IsAny<SpeechGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Model3DAssetAgent_generates_from_reference_image_when_present()
    {
        var modelFile = Path.Combine(Path.GetTempPath(), "nexo-3d-" + Guid.NewGuid() + ".glb");
        var imageFile = Path.Combine(Path.GetTempPath(), "nexo-ref-" + Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(modelFile, [1, 2, 3]);
        await File.WriteAllBytesAsync(imageFile, [9, 9, 9]);

        try
        {
            var gen = new Mock<IModel3DGenerator>();
            gen.Setup(g => g.GenerateFromImageAsync(imageFile, It.IsAny<Model3DGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Generated3DModel
                {
                    FilePath = modelFile,
                    Format = Model3DFormat.GLB,
                    VertexCount = 100,
                    TriangleCount = 50,
                });

            var agent = new Model3DAssetAgent(
                new AgentSpawnSpec { AgentId = "model-1", Domain = "Assets", Goal = "Hero mesh" },
                gen.Object,
                model: null,
                Mock.Of<IAssetStorage>(),
                NullLogger<BaseAgent>.Instance);

            var asset = await InvokeGenerateAssetAsync(agent, new GenerationPrompt
            {
                TextPrompt = "hero",
                Parameters = new Dictionary<string, object>
                {
                    ["referenceImage"] = imageFile,
                    ["generateTextures"] = true,
                    ["polyCount"] = 500,
                },
            }, CancellationToken.None);

            asset.FilePath.Should().Be(modelFile);
            gen.Verify(g => g.GenerateFromImageAsync(imageFile, It.IsAny<Model3DGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(modelFile)) File.Delete(modelFile);
            if (File.Exists(imageFile)) File.Delete(imageFile);
        }
    }

    [Fact]
    public async Task InProcessAgentHandle_exposes_container_operations()
    {
        var container = CreateGenericContainer("handle-1");
        await container.InitializeAsync();
        var handle = new InProcessAgentHandle(container);

        handle.AgentId.Should().Be("handle-1");
        handle.State.Should().Be(AgentState.Ready);
        (await handle.ExecuteAsync()).Should().NotBeNull();
        await handle.ShutdownAsync();
        handle.State.Should().Be(AgentState.Terminated);
        handle.Terminate();
    }

    [Fact]
    public async Task AudioAssetAgent_evaluates_duration_and_format_constraints()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-aud-constraint-" + Guid.NewGuid() + ".mp3");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            var audioGen = new Mock<IAudioGenerator>();
            audioGen.Setup(g => g.GenerateAsync(It.IsAny<AudioGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GeneratedAudio
                {
                    FilePath = tempFile,
                    MimeType = "audio/mpeg",
                    DurationMs = 20_000,
                    SampleRate = 44_100,
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempFile);

            var agent = new AudioAssetAgent(
                new AgentSpawnSpec
                {
                    AgentId = "audio-constraints",
                    Domain = "Assets",
                    Goal = "SFX",
                    Constraints = new[]
                    {
                        new AgentConstraint { Type = "duration", Description = "duration 10 seconds mandatory", IsMandatory = true },
                        new AgentConstraint { Type = "format", Description = "must be wav", IsMandatory = false },
                    },
                },
                audioGen.Object,
                model: null,
                storage.Object,
                NullLogger<BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = (AssetOutput)await agent.ExecuteAsync();
            output.Validation!.Warnings.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Model3DAssetAgent_evaluates_polygon_and_texture_constraints()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-3d-constraint-" + Guid.NewGuid() + ".fbx");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            var gen = new Mock<IModel3DGenerator>();
            gen.Setup(g => g.GenerateFromTextAsync(It.IsAny<Model3DGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Generated3DModel
                {
                    FilePath = tempFile,
                    Format = Model3DFormat.FBX,
                    VertexCount = 100,
                    TriangleCount = 10_000,
                    TexturePaths = Array.Empty<string>(),
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempFile);

            var agent = new Model3DAssetAgent(
                new AgentSpawnSpec
                {
                    AgentId = "model-constraints",
                    Domain = "Assets",
                    Goal = "Prop",
                    Constraints = new[]
                    {
                        new AgentConstraint { Type = "poly", Description = "maximum 500 triangles", IsMandatory = true },
                        new AgentConstraint { Type = "texture", Description = "textures required", IsMandatory = true },
                    },
                },
                gen.Object,
                model: null,
                storage.Object,
                NullLogger<BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = (AssetOutput)await agent.ExecuteAsync();
            output.Validation!.IsValid.Should().BeFalse();
            output.Validation.FailedChecks.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImageAssetAgent_refines_prompt_when_model_reports_style_failure()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-img-style-" + Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3]);

        try
        {
            var callCount = 0;
            var imageGen = new Mock<IImageGenerator>();
            imageGen.Setup(g => g.GenerateAsync(It.IsAny<ImageGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return new GeneratedImage
                    {
                        FilePath = tempFile,
                        Size = ImageSize.Square1024,
                        MimeType = callCount == 1 ? "image/jpeg" : "image/png",
                    };
                });
            imageGen.Setup(g => g.SupportedSizes).Returns(new[] { ImageSize.Square1024, ImageSize.Square512 });
            imageGen.Setup(g => g.SupportedStyles).Returns(new[] { "default" });

            var model = new Mock<IModel>();
            model.SetupSequence(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModelOutput("""{"prompt":"hero icon","style":"flat"}"""))
                .ReturnsAsync(new ModelOutput("refined flat icon with correct png style"));

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempFile);

            var agent = new ImageAssetAgent(
                new AgentSpawnSpec
                {
                    AgentId = "img-model",
                    Domain = "Assets",
                    Goal = "Create icon",
                    Description = "Flat UI icon",
                    Constraints = new[]
                    {
                        new AgentConstraint { Type = "style", Description = "must match flat style", IsMandatory = true },
                        new AgentConstraint { Type = "format", Description = "must be png", IsMandatory = true },
                    },
                },
                imageGen.Object,
                model.Object,
                storage.Object,
                NullLogger<BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = (AssetOutput)await agent.ExecuteAsync();
            output.Validation.Should().NotBeNull();
            output.Validation!.IsValid.Should().BeTrue();
            callCount.Should().BeGreaterThan(1);
            model.Verify(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private static AgentContainer CreateGenericContainer(string agentId, string goal = "work")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        return factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = agentId,
            Domain = "Planning",
            Goal = goal,
        });
    }

    private static void SetAgentState(BaseAgent agent, AgentState state)
    {
        var field = typeof(BaseAgent).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(agent, state);
    }

    private static async Task<GeneratedAssetBase> InvokeGenerateAssetAsync(BaseAssetAgent agent, GenerationPrompt prompt, CancellationToken ct)
    {
        var method = typeof(BaseAssetAgent).GetMethod(
            "GenerateAssetAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<GeneratedAssetBase>)method.Invoke(agent, new object[] { prompt, ct })!;
        return await task;
    }

    private sealed class FailingShutdownAgent : BaseAgent
    {
        public FailingShutdownAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken) => Task.FromResult<object>("ok");
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("shutdown failed");
    }

    private sealed class FailingExecuteAgent : BaseAgent
    {
        public FailingExecuteAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger) : base(spec, logger) { }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task<object> OnExecuteAsync(IReadOnlyDictionary<string, object>? dependencyOutputs, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("execute failed");
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
