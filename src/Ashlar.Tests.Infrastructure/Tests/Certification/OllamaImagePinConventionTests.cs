using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Compose stacks must not float on <c>ollama/ollama:latest</c>. The pin lives
/// in <c>ci/ollama-image</c> (tag + multi-arch index digest) and is the default
/// for <c>ASHLAR_OLLAMA_IMAGE</c>.
/// </summary>
[Trait("Category", "Certification")]
public sealed class OllamaImagePinConventionTests
{
    [Fact]
    public void CiOllamaImage_IsAVersionedDigestPin()
    {
        var pin = ReadPin();
        pin.Should().StartWith("ollama/ollama:");
        pin.Should().Contain("@sha256:");
        pin.Should().NotContain(":latest");
        pin.Should().NotBe("ollama/ollama");
    }

    [Fact]
    public void DeployCompose_DefaultsToTheCiOllamaPin_AndNeverLatest()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var pin = ReadPin();
        var composeDir = Path.Combine(root, "deploy", "compose");
        var files = Directory.GetFiles(composeDir, "docker-compose*.yml");
        files.Should().NotBeEmpty();

        var imageFiles = 0;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("ollama/ollama", StringComparison.Ordinal))
                continue;

            imageFiles++;
            text.Should().NotContain("ollama/ollama:latest",
                "{0} must not float on :latest", file);
            text.Should().NotContain("image: ollama/ollama\n",
                "{0} must not use an untagged ollama/ollama image", file);
            text.Should().Contain(pin,
                "{0} must default ASHLAR_OLLAMA_IMAGE to {1}", file, pin);
        }

        imageFiles.Should().BeGreaterThanOrEqualTo(6,
            "every stack that pulls ollama/ollama must keep the ci/ollama-image default");
    }

    private static string ReadPin()
    {
        var path = Path.Combine(RepoPathResolver.FindRepoRoot(), "ci/ollama-image");
        return File.ReadAllText(path).Trim();
    }
}
