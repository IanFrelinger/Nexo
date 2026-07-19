using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Models;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Models;

public sealed class DeterministicToolSequenceModelTests
{
    [Fact]
    public async Task CompleteAsync_EmitsStepsThenEmptyToolCalls()
    {
        var model = DeterministicToolSequenceModel.FromSteps(
        [
            ToolSequenceStep.FromObject("game.launch", new { }),
            ToolSequenceStep.FromObject("game.status", new { label = "initial" }),
            ToolSequenceStep.FromObject("game.stop", new { })
        ]);

        var first = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        using (var doc = JsonDocument.Parse(first.Text))
        {
            Assert.Equal(
                "game.launch",
                doc.RootElement.GetProperty("tool_calls")[0].GetProperty("id").GetString());
        }

        var second = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        using (var doc = JsonDocument.Parse(second.Text))
        {
            Assert.Equal(
                "game.status",
                doc.RootElement.GetProperty("tool_calls")[0].GetProperty("id").GetString());
        }

        _ = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        var done = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        using (var doc = JsonDocument.Parse(done.Text))
        {
            Assert.Equal(0, doc.RootElement.GetProperty("tool_calls").GetArrayLength());
        }
    }

    [Fact]
    public async Task CompleteAsync_SkipsStepsWhenPredicateFalse()
    {
        var model = new DeterministicToolSequenceModel(
        [
            new ToolSequenceTurn(
                "optional video",
                [
                    ToolSequenceStep.FromObject(
                        "game.video_start",
                        new { },
                        includeWhen: static () => false),
                    ToolSequenceStep.FromObject("game.status", new { label = "ok" })
                ])
        ]);

        var output = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        using var doc = JsonDocument.Parse(output.Text);
        var calls = doc.RootElement.GetProperty("tool_calls");
        Assert.Equal(1, calls.GetArrayLength());
        Assert.Equal("game.status", calls[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task FromJson_LoadsMultiCallTurn()
    {
        const string json =
            """
            [
              {
                "rationale": "boot",
                "tool_calls": [
                  { "id": "game.launch", "arguments": {} },
                  { "id": "game.status", "arguments": { "label": "initial" } }
                ]
              }
            ]
            """;
        var model = DeterministicToolSequenceModel.FromJson(json);
        var output = await model.CompleteAsync(EmptyInput(), CancellationToken.None);
        using var doc = JsonDocument.Parse(output.Text);
        Assert.Equal(2, doc.RootElement.GetProperty("tool_calls").GetArrayLength());
        Assert.Equal("boot", doc.RootElement.GetProperty("rationale").GetString());
    }

    private static ModelInput EmptyInput()
        => new(Array.Empty<(string role, string content)>());
}
