using System.Text.Json;
using FluentAssertions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Nexo.Mcp.Client.Tests;

public sealed class McpJsonBridgeTests
{
    private static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Object_arguments_become_a_dictionary_of_elements()
    {
        var args = Parse("""{"path":"a.txt","count":3}""");

        var dictionary = McpJsonBridge.ToArgumentDictionary(args);

        dictionary.Should().HaveCount(2);
        ((JsonElement)dictionary["path"]!).GetString().Should().Be("a.txt");
        ((JsonElement)dictionary["count"]!).GetInt32().Should().Be(3);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"text\"")]
    [InlineData("[1,2]")]
    [InlineData("null")]
    public void Non_object_arguments_become_an_empty_dictionary(string json)
    {
        McpJsonBridge.ToArgumentDictionary(Parse(json)).Should().BeEmpty();
    }

    [Fact]
    public void Text_blocks_join_into_the_text_payload()
    {
        var result = new CallToolResult
        {
            Content =
            [
                new TextContentBlock { Text = "line one" },
                new TextContentBlock { Text = "line two" },
            ],
        };

        var mapped = McpJsonBridge.ToToolResult(result, "mcp:s:t", tick: 5);

        mapped.Payload.Should().Be("line one\nline two");
        mapped.Delta.TickFrom.Should().Be(5);
        mapped.Delta.TickTo.Should().Be(6);
        mapped.Delta.Log.Should().ContainSingle().Which.Should().Contain("ok");
    }

    [Fact]
    public void Structured_content_wins_over_text_as_payload()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{\"answer\":42}" }],
            StructuredContent = Parse("""{"answer":42}"""),
        };

        var mapped = McpJsonBridge.ToToolResult(result, "mcp:s:t", tick: 0);

        mapped.Payload.Should().BeOfType<JsonElement>()
            .Which.GetProperty("answer").GetInt32().Should().Be(42);
    }

    [Fact]
    public void Remote_isError_maps_to_a_failure_payload_not_an_exception()
    {
        var result = new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "remote exploded" }],
        };

        var mapped = McpJsonBridge.ToToolResult(result, "mcp:s:t", tick: 0);

        var failure = mapped.Payload.Should().BeOfType<McpToolFailure>().Subject;
        failure.Error.Should().Contain("remote exploded");
        failure.IsError.Should().BeTrue();
        mapped.Delta.Log.Should().ContainSingle().Which.Should().Contain("error");
    }

    [Fact]
    public void Remote_isError_with_no_text_still_reports_an_error()
    {
        var result = new CallToolResult { IsError = true, Content = [] };

        var mapped = McpJsonBridge.ToToolResult(result, "mcp:s:t", tick: 0);

        mapped.Payload.Should().BeOfType<McpToolFailure>()
            .Which.Error.Should().NotBeNullOrWhiteSpace();
    }
}
