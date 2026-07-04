using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.MultiModal;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration multi modal.</summary>
public class OrchestrationMultiModalTests
{
    /// <summary>Test multi modal agent.</summary>
    private sealed class TestMultiModalAgent : MultiModalAgentBase
    {
        public TestMultiModalAgent(AgentSpawnSpec spec, MultiModalProcessor? processor = null)
            : base(spec, NullLogger<BaseAgent>.Instance, processor: processor)
        {
        }

        /// <summary>On initialize async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On dependencies resolved async.</summary>
        /// <param name="dependencyOutputs">Dependency outputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnDependenciesResolvedAsync(IReadOnlyDictionary<string, object> dependencyOutputs, CancellationToken cancellationToken) => Task.CompletedTask;
        /// <summary>On shutdown async.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task OnShutdownAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override async Task<object> OnExecuteAsync(
            IReadOnlyDictionary<string, object>? dependencyOutputs,
            CancellationToken cancellationToken)
        {
            var input = await ProcessInputAsync(dependencyOutputs, cancellationToken);
            var prompt = BuildMultiModalPrompt("Analyze:", input);
            return prompt;
        }
    }

    [Fact]
    public async Task MultiModalProcessor_passes_through_input()
    {
        var input = new MultiModalInput
        {
            TextInputs = new[] { "hello" },
            ImageInputs = new[] { new ImageInput { Source = "img", Data = "data:image/png;base64,abc", Format = "png" } },
            AudioInputs = new[] { new AudioInput { Source = "aud", Data = "data:audio/wav;base64,xyz", Format = "wav" } },
        };
        var result = await new MultiModalProcessor(NullLogger<MultiModalProcessor>.Instance)
            .ProcessAsync(input, CancellationToken.None);
        result.TextInputs.Should().ContainSingle();
        result.ImageInputs.Should().ContainSingle();
        result.AudioInputs.Should().ContainSingle();
    }

    [Fact]
    public async Task TestMultiModalAgent_parses_json_string_and_data_uri_dependencies()
    {
        var spec = new AgentSpawnSpec { AgentId = "mm-1", Domain = "MultiModal", Goal = "Analyze" };
        var agent = new TestMultiModalAgent(spec, new MultiModalProcessor());
        await agent.InitializeAsync();

        var deps = new Dictionary<string, object>
        {
            ["json"] = JsonDocument.Parse("""
                {"text":"hello","image":"data:image/jpeg;base64,/9j/4AAQ","audio":"data:audio/mp3;base64,SUQz"}
                """).RootElement,
            ["plain"] = "fallback text",
            ["imgOnly"] = "data:image/png;base64,iVBORw0KGgo",
            ["audOnly"] = "data:audio/wav;base64,UklGR",
        };

        var prompt = (string)await agent.ExecuteAsync(deps);
        prompt.Should().Contain("Analyze:");
        prompt.Should().Contain("Text Inputs");
        prompt.Should().Contain("Image Inputs");
        prompt.Should().Contain("Audio Inputs");
    }
}
