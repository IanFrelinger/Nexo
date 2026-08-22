using System.Text.Json;
using A2A;
using FluentAssertions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Transport;
using Xunit;
using A2ATaskStatus = A2A.TaskStatus;

namespace Ashlar.Transport.A2A.Tests;

public sealed class A2AInvocationMapperTests
{
    private static AgentInvocationRequest Request(
        IReadOnlyDictionary<string, object?>? payload = null,
        string? spanId = null)
        => new("target-agent", "corr-42", spanId, payload);

    [Fact]
    public void BuildMessage_carries_payload_as_data_part_and_context_as_metadata()
    {
        var request = Request(new Dictionary<string, object?> { ["objective"] = "scan", ["depth"] = 3 }, spanId: "span-7");

        var message = A2AInvocationMapper.BuildMessage(request, barrier: null);

        message.Role.Should().Be(Role.User);
        var dataPart = message.Parts!.Single(p => p.ContentCase == PartContentCase.Data);
        dataPart.Data!.Value.GetProperty("objective").GetString().Should().Be("scan");
        dataPart.Data!.Value.GetProperty("depth").GetInt32().Should().Be(3);
        message.Metadata![A2AInvocationMapper.CorrelationIdKey].GetString().Should().Be("corr-42");
        message.Metadata![A2AInvocationMapper.SpanIdKey].GetString().Should().Be("span-7");
    }

    [Fact]
    public void BuildMessage_surfaces_a_text_part_when_payload_has_a_message_string()
    {
        var request = Request(new Dictionary<string, object?> { ["message"] = "please scan the repo" });

        var message = A2AInvocationMapper.BuildMessage(request, barrier: null);

        message.Parts!.First().ContentCase.Should().Be(PartContentCase.Text);
        message.Parts!.First().Text.Should().Be("please scan the repo");
    }

    [Fact]
    public void BuildMessage_propagates_barrier_context_as_metadata()
    {
        var hierarchy = new BarrierHierarchy(new[] { new BarrierLevel("public", 0), new BarrierLevel("internal", 1) });
        var barrier = BarrierContext.Create("internal", "api-key", "test-agent", "corr-42", hierarchy);

        var message = A2AInvocationMapper.BuildMessage(Request(), barrier);

        message.Metadata![A2AInvocationMapper.BarrierLevelKey].GetString().Should().Be("internal");
        message.Metadata![A2AInvocationMapper.BarrierSourceKey].GetString().Should().Be("api-key");
    }

    [Fact]
    public void Completed_task_maps_to_success_with_artifact_data_output()
    {
        var response = new SendMessageResponse
        {
            Task = new AgentTask
            {
                Id = "t1",
                Status = new A2ATaskStatus { State = TaskState.Completed },
                Artifacts =
                [
                    new Artifact { Parts = [Part.FromData(JsonSerializer.SerializeToElement(new { answer = 42 }))] },
                ],
            },
        };

        var result = A2AInvocationMapper.MapResponse(response, Request(), TimeSpan.FromSeconds(1));

        result.Success.Should().BeTrue();
        result.Output.Should().BeOfType<JsonElement>()
            .Which.GetProperty("answer").GetInt32().Should().Be(42);
        result.CorrelationId.Should().Be("corr-42");
    }

    [Theory]
    [InlineData(TaskState.Failed, "a2a.task.failed")]
    [InlineData(TaskState.Canceled, "a2a.task.canceled")]
    [InlineData(TaskState.Rejected, "a2a.task.rejected")]
    [InlineData(TaskState.InputRequired, "a2a.task.input-required")]
    [InlineData(TaskState.AuthRequired, "a2a.task.auth-required")]
    [InlineData(TaskState.Working, "a2a.task.incomplete")]
    public void Non_completed_terminal_states_map_to_typed_failures(TaskState state, string expectedCode)
    {
        var response = new SendMessageResponse
        {
            Task = new AgentTask { Id = "t1", Status = new A2ATaskStatus { State = state } },
        };

        var result = A2AInvocationMapper.MapResponse(response, Request(), TimeSpan.Zero);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(expectedCode);
    }

    [Fact]
    public void Failure_status_message_text_becomes_the_error_message()
    {
        var response = new SendMessageResponse
        {
            Task = new AgentTask
            {
                Id = "t1",
                Status = new A2ATaskStatus
                {
                    State = TaskState.Failed,
                    Message = new Message { Role = Role.Agent, Parts = [Part.FromText("budget exceeded")] },
                },
            },
        };

        var result = A2AInvocationMapper.MapResponse(response, Request(), TimeSpan.Zero);

        result.ErrorMessage.Should().Contain("budget exceeded");
    }

    [Fact]
    public void Message_response_maps_to_success_with_text_output()
    {
        var response = new SendMessageResponse
        {
            Message = new Message { Role = Role.Agent, Parts = [Part.FromText("hello back")] },
        };

        var result = A2AInvocationMapper.MapResponse(response, Request(), TimeSpan.Zero);

        result.Success.Should().BeTrue();
        result.Output.Should().Be("hello back");
    }

    [Fact]
    public void Empty_response_is_a_typed_failure()
    {
        var result = A2AInvocationMapper.MapResponse(new SendMessageResponse(), Request(), TimeSpan.Zero);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("a2a.response.empty");
    }
}
