using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Assets;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Reflection;
using Xunit;
using Ashlar.Orchestration.GameDomain;
using Ashlar.Orchestration.GameDomain.Agents.Assets;

namespace Ashlar.Tests.Orchestration;

/// <summary>
/// Gap coverage for the generative asset agents, split out of
/// OrchestrationAgentsGapCoverageTests.
///
/// The asset PORTS and BaseAssetAgent stay in the kernel; only these three concrete
/// agents are game-flavoured and move with the game layer. The seven lifecycle and
/// BaseAgent tests they were interleaved with stay put.
/// </summary>
public class GameAssetAgentGapTests
{
    [Fact]
    public async Task AudioAssetAgent_generates_speech_when_voice_type_requested()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "ashlar-speech-" + Guid.NewGuid() + ".wav");
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
        var modelFile = Path.Combine(Path.GetTempPath(), "ashlar-3d-" + Guid.NewGuid() + ".glb");
        var imageFile = Path.Combine(Path.GetTempPath(), "ashlar-ref-" + Guid.NewGuid() + ".png");
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
    public async Task AudioAssetAgent_evaluates_duration_and_format_constraints()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "ashlar-aud-constraint-" + Guid.NewGuid() + ".mp3");
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
        var tempFile = Path.Combine(Path.GetTempPath(), "ashlar-3d-constraint-" + Guid.NewGuid() + ".fbx");
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
        var tempFile = Path.Combine(Path.GetTempPath(), "ashlar-img-style-" + Guid.NewGuid() + ".png");
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


    [Fact]
    public void A_claimed_but_unbuildable_asset_domain_throws()
    {
        // Moved from OrchestrationFactoryAndCommunicationTests, which stays in the kernel.
        // "shader" and "animation" are claimed by GameAssetAgentProvider.Handles but have no
        // case in Create, so they throw — carried over verbatim from AgentFactory.IsAssetDomain,
        // which had the same advertise-then-reject shape. The assertion only holds with the
        // provider registered; without it "shader" is merely an unrecognised domain and
        // returns a GenericAgent, which is why this test could not stay behind.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IModel>());
        services.AddSingleton(Mock.Of<IImageGenerator>());
        services.AddSingleton(Mock.Of<IAudioGenerator>());
        services.AddSingleton(Mock.Of<IModel3DGenerator>());
        services.AddSingleton(Mock.Of<IAssetStorage>());
        services.AddSingleton<AgentFactory>();
        services.AddGameAssetAgents();
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<AgentFactory>();
        var act = () => factory.CreateAgent(new AgentSpawnSpec
        {
            AgentId = "shader-1",
            Domain = "shader",
            Goal = "Write a shader",
        });

        act.Should().Throw<ArgumentException>();
    }

    // Moved with the tests that use it — it became dead in
    // OrchestrationAgentsGapCoverageTests once the five asset tests left.

    private static async Task<GeneratedAssetBase> InvokeGenerateAssetAsync(BaseAssetAgent agent, GenerationPrompt prompt, CancellationToken ct)
    {
        var method = typeof(BaseAssetAgent).GetMethod(
            "GenerateAssetAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<GeneratedAssetBase>)method.Invoke(agent, new object[] { prompt, ct })!;
        return await task;
    }


    [Theory]
    [InlineData("image", typeof(ImageAssetAgent))]
    [InlineData("texture", typeof(ImageAssetAgent))]
    [InlineData("audio", typeof(AudioAssetAgent))]
    [InlineData("sound", typeof(AudioAssetAgent))]
    [InlineData("music", typeof(AudioAssetAgent))]
    [InlineData("model3d", typeof(Model3DAssetAgent))]
    [InlineData("3d", typeof(Model3DAssetAgent))]
    [InlineData("model", typeof(Model3DAssetAgent))]
    public void AgentFactory_routes_asset_domains_through_the_provider(string domain, Type expected)
    {
        // Replaces the three asset InlineData rows deleted from
        // OrchestrationFactoryAndCommunicationTests, and widens them: the kernel theory only
        // covered image/audio/model3d, leaving the texture/sound/music/3d/model aliases
        // untested even though AgentFactory claimed them.
        using var provider = AssetServices(withProvider: true);

        provider.GetRequiredService<AgentFactory>()
            .CreateAgent(SpecFor(domain)).Should().BeOfType(expected);
    }

    [Fact]
    public void Without_the_game_layer_asset_domains_fall_back_to_generic()
    {
        using var provider = AssetServices(withProvider: false);

        provider.GetRequiredService<AgentFactory>()
            .CreateAgent(SpecFor("image")).Should().BeOfType<GenericAgent>();
    }

    private static AgentSpawnSpec SpecFor(string domain) => new()
    {
        AgentId = $"{domain}-1",
        Domain = domain,
        Goal = $"Generate {domain}",
    };

    private static ServiceProvider AssetServices(bool withProvider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IModel>());
        services.AddSingleton(Mock.Of<IImageGenerator>());
        services.AddSingleton(Mock.Of<IAudioGenerator>());
        services.AddSingleton(Mock.Of<IModel3DGenerator>());
        services.AddSingleton(Mock.Of<IAssetStorage>());
        services.AddSingleton<AgentFactory>();
        if (withProvider)
        {
            services.AddGameAssetAgents();
        }

        return services.BuildServiceProvider();
    }
}
