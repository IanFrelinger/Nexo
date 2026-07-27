using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class OllamaModelPinTests : IDisposable
{
    private readonly string? _model;
    private readonly string? _digest;

    public OllamaModelPinTests()
    {
        _model = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        _digest = Environment.GetEnvironmentVariable("OLLAMA_MODEL_DIGEST");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("OLLAMA_MODEL", _model);
        Environment.SetEnvironmentVariable("OLLAMA_MODEL_DIGEST", _digest);
    }

    [Fact]
    public void ParseConfigured_tag_only()
    {
        Environment.SetEnvironmentVariable("OLLAMA_MODEL_DIGEST", null);
        var pin = OllamaModelPin.ParseConfigured("qwen2.5-coder:7b");
        pin.Tag.Should().Be("qwen2.5-coder:7b");
        pin.Digest.Should().BeNull();
        pin.Display.Should().Be("qwen2.5-coder:7b");
    }

    [Fact]
    public void ParseConfigured_tag_at_digest()
    {
        Environment.SetEnvironmentVariable("OLLAMA_MODEL_DIGEST", null);
        var pin = OllamaModelPin.ParseConfigured(
            "qwen2.5-coder:7b@sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        pin.Tag.Should().Be("qwen2.5-coder:7b");
        pin.Digest.Should().Be(
            "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
    }

    [Fact]
    public void ParseConfigured_digest_env_wins()
    {
        Environment.SetEnvironmentVariable("OLLAMA_MODEL", "mistral:7b");
        Environment.SetEnvironmentVariable(
            "OLLAMA_MODEL_DIGEST",
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var pin = OllamaModelPin.ParseConfigured(null);
        pin.Tag.Should().Be("mistral:7b");
        pin.Digest.Should().Be(
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    }
}
