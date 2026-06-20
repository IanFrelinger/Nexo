using FluentAssertions;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Generation.Claude;
using Nexo.Spike.S3.Models;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class RequestIsolationTests
{
    [Fact]
    public void Sealed_request_json_contains_no_oracle_test_property_or_acceptance_content()
    {
        var intent = new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference");
        var generator = new RecordedSkillGenerator();
        var handoff = generator.Describe(intent);

        var requestJson = File.ReadAllText(handoff.RequestPath);
        RequestIsolationGuard.AssertIsolated(requestJson, "sealed request.json");
        requestJson.Should().NotContain("acceptanceCriteria");
        requestJson.Should().NotContain("FluentAssertions");
        requestJson.Should().NotContain("reference-oracle");
    }

    [Fact]
    public async Task Assembled_Claude_prompt_contains_no_oracle_test_property_or_acceptance_content()
    {
        var intent = new IntentDescriptor("csv-column-type-inference", ["live"], CapabilityKey: "csv-type-inference");
        var (request, _) = await GenerationRequestSealer.BuildAsync(intent, [], 0);
        var prompt = SkillGeneratorPromptAssembler.Assemble(request);

        RequestIsolationGuard.AssertIsolated(prompt.SystemPrompt, "assembled system prompt");
        RequestIsolationGuard.AssertIsolated(prompt.UserPrompt, "assembled user prompt");

        prompt.UserPrompt.Should().NotContain("acceptanceCriteria");
        prompt.UserPrompt.Should().NotContain("ColumnTypeInferrerRedTests");
        prompt.UserPrompt.Should().NotContain("MetamorphicPropertyTests");
        prompt.UserPrompt.Should().NotContain("held-out");
    }

    [Fact]
    public void Claude_Describe_persists_isolated_prompt_artifacts()
    {
        var generator = new ClaudeSkillGenerator(new FakeS3LlmTransport(), workRoot: CreateTempWorkRoot());
        var intent = new IntentDescriptor("csv-column-type-inference", ["live"], CapabilityKey: "csv-type-inference");
        var handoff = generator.Describe(intent);

        handoff.IsolationEnforced.Should().BeTrue();
        File.Exists(Path.Combine(handoff.WorkDirectory, "assembled-system-prompt.txt")).Should().BeTrue();
        File.Exists(Path.Combine(handoff.WorkDirectory, "assembled-user-prompt.txt")).Should().BeTrue();

        var system = File.ReadAllText(Path.Combine(handoff.WorkDirectory, "assembled-system-prompt.txt"));
        var user = File.ReadAllText(Path.Combine(handoff.WorkDirectory, "assembled-user-prompt.txt"));
        RequestIsolationGuard.AssertIsolated(system + user, "claude persisted prompts");
    }

    private static string CreateTempWorkRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-s3-isolation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    internal sealed class FakeS3LlmTransport : IS3LlmTransport
    {
        public int CallCount { get; private set; }

        public Task<string> CompleteAsync(S3LlmCompletionRequest request, CancellationToken ct = default)
        {
            CallCount++;
            RequestIsolationGuard.AssertIsolated(request.SystemPrompt + request.UserPrompt, "fake transport prompt");
            return Task.FromResult(
                """
                ```csharp
                namespace CsvColumnInferrer;
                public enum ColumnType { String, Integer, Decimal, Boolean, Date }
                public static class ColumnTypeInferrer
                {
                    public static ColumnType InferType(IReadOnlyList<string> values) => ColumnType.String;
                }
                ```
                """);
        }
    }
}
