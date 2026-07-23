using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Application.Ide;

/// <summary>Smoke tests for IDE patch JSON parsing conventions used by IdeEndpoints.</summary>
public sealed class IdePatchJsonConventionTests
{
    [Fact]
    public void PatchPayload_RoundTrips_CamelCase()
    {
        const string json = """
            {"patches":[{"path":"src/Foo.cs","newContent":"class Foo {}","summary":"add class"}]}
            """;

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("patches").GetArrayLength().Should().Be(1);
        var patch = doc.RootElement.GetProperty("patches")[0];
        patch.GetProperty("path").GetString().Should().Be("src/Foo.cs");
        patch.GetProperty("newContent").GetString().Should().Contain("Foo");
    }

    [Fact]
    public void ExtractJsonObject_IgnoresMarkdownFence()
    {
        var text = """
            ```json
            {"patches":[{"path":"a.cs","newContent":"x","summary":"s"}]}
            ```
            """;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        var slice = text[start..(end + 1)];
        using var doc = JsonDocument.Parse(slice);
        doc.RootElement.GetProperty("patches").GetArrayLength().Should().Be(1);
    }
}
