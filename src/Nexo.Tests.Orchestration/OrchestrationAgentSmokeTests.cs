using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents.Assets;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using Xunit;

namespace Nexo.Tests.Orchestration;

public class OrchestrationAgentSmokeTests
{
    [Fact]
    public async Task ImageAssetAgent_generates_asset_without_llm_model()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-img-" + Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3, 4 });

        try
        {
            var imageGen = new Mock<IImageGenerator>();
            imageGen.Setup(g => g.GenerateAsync(It.IsAny<ImageGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GeneratedImage
                {
                    FilePath = tempFile,
                    Size = ImageSize.Square1024,
                    MimeType = "image/png",
                });
            imageGen.Setup(g => g.SupportedSizes).Returns(new[] { ImageSize.Square1024 });
            imageGen.Setup(g => g.SupportedStyles).Returns(new[] { "default" });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/stored/" + Path.GetFileName(tempFile));

            var spec = new AgentSpawnSpec
            {
                AgentId = "img-1",
                Domain = "Assets",
                Goal = "Create icon",
                Description = "Game UI icon",
            };

            var agent = new ImageAssetAgent(
                spec,
                imageGen.Object,
                model: null,
                storage.Object,
                NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = await agent.ExecuteAsync();
            output.Should().BeOfType<AssetOutput>();
            ((AssetOutput)output).AssetType.Should().Be(AssetType.Image);
            ((AssetOutput)output).FilePath.Should().Contain(".png");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AudioAssetAgent_generates_music_without_llm_model()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-aud-" + Guid.NewGuid() + ".wav");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            var audioGen = new Mock<IAudioGenerator>();
            audioGen.Setup(g => g.GenerateAsync(It.IsAny<AudioGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GeneratedAudio
                {
                    FilePath = tempFile,
                    MimeType = "audio/wav",
                    DurationMs = 3000,
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/stored/" + Path.GetFileName(tempFile));

            var spec = new AgentSpawnSpec
            {
                AgentId = "aud-1",
                Domain = "Assets",
                Goal = "Create sfx",
                Description = "Explosion sound",
            };

            var agent = new AudioAssetAgent(
                spec,
                audioGen.Object,
                model: null,
                storage.Object,
                NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = await agent.ExecuteAsync();
            output.Should().BeOfType<AssetOutput>();
            ((AssetOutput)output).AssetType.Should().Be(AssetType.Audio);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Model3DAssetAgent_generates_model_without_llm()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-3d-" + Guid.NewGuid() + ".glb");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

        try
        {
            var gen = new Mock<IModel3DGenerator>();
            gen.Setup(g => g.GenerateFromTextAsync(It.IsAny<Model3DGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Generated3DModel
                {
                    FilePath = tempFile,
                    Format = Model3DFormat.GLB,
                    VertexCount = 100,
                    TriangleCount = 50,
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/stored/model.glb");

            var spec = new AgentSpawnSpec
            {
                AgentId = "3d-1",
                Domain = "Assets",
                Goal = "Create prop",
                Description = "Tree model",
            };

            var agent = new Model3DAssetAgent(
                spec,
                gen.Object,
                model: null,
                storage.Object,
                NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = await agent.ExecuteAsync();
            output.Should().BeOfType<AssetOutput>();
            ((AssetOutput)output).AssetType.Should().Be(AssetType.Model3D);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImageAssetAgent_retries_when_resolution_constraint_fails()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-img-" + Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3, 4 });

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
                        Size = callCount == 1 ? ImageSize.Square1024 : ImageSize.Square512,
                        MimeType = callCount == 1 ? "image/jpeg" : "image/png",
                    };
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/stored/icon.png");

            var spec = new AgentSpawnSpec
            {
                AgentId = "img-retry",
                Domain = "Assets",
                Goal = "Create icon",
                Description = "UI icon",
                Constraints = new[]
                {
                    new AgentConstraint { Type = "size", Description = "resolution 512x512", IsMandatory = true },
                    new AgentConstraint { Type = "format", Description = "must be png", IsMandatory = true },
                },
            };

            var agent = new ImageAssetAgent(
                spec,
                imageGen.Object,
                model: null,
                storage.Object,
                NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance);

            await agent.InitializeAsync();
            var output = await agent.ExecuteAsync();
            output.Should().BeOfType<AssetOutput>();
            callCount.Should().BeGreaterThan(1);
            ((AssetOutput)output).MimeType.Should().Be("image/png");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImageAssetAgent_honors_width_height_parameters()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nexo-img-" + Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1 });

        try
        {
            ImageGenerationRequest? captured = null;
            var imageGen = new Mock<IImageGenerator>();
            imageGen.Setup(g => g.GenerateAsync(It.IsAny<ImageGenerationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ImageGenerationRequest, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(new GeneratedImage
                {
                    FilePath = tempFile,
                    Size = ImageSize.Landscape1024x768,
                    MimeType = "image/png",
                });

            var storage = new Mock<IAssetStorage>();
            storage.Setup(s => s.StoreAsync(tempFile, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/stored/wide.png");

            var spec = new AgentSpawnSpec
            {
                AgentId = "img-size",
                Domain = "Assets",
                Goal = "Banner",
                Description = "Wide banner",
                Constraints = new[]
                {
                    new AgentConstraint { Type = "size", Description = "resolution 1024x768", IsMandatory = false },
                },
            };

            var agent = new ImageAssetAgent(
                spec,
                imageGen.Object,
                model: null,
                storage.Object,
                NullLogger<Nexo.Orchestration.Agents.BaseAgent>.Instance);

            await agent.InitializeAsync();
            await agent.ExecuteAsync();

            captured.Should().NotBeNull();
            captured!.Size.Should().Be(ImageSize.Landscape1024x768);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
