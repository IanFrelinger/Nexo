using FluentAssertions;
using Xunit;

namespace Ashlar.Mcp.Server.Tests;

public sealed class McpToolNamingTests
{
    [Theory]
    [InlineData("repo.fs.read", "repo_fs_read")]
    [InlineData("already_safe-Name123", "already_safe-Name123")]
    [InlineData("web search!", "web_search_")]
    [InlineData("a:b/c", "a_b_c")]
    public void Sanitize_maps_unsafe_characters_to_underscores(string canonicalId, string expected)
    {
        McpToolNaming.Sanitize(canonicalId).Should().Be(expected);
    }

    [Fact]
    public void Sanitize_trims_to_max_length()
    {
        var longId = new string('a', McpToolNaming.MaxNameLength + 20);

        var name = McpToolNaming.Sanitize(longId);

        name.Should().HaveLength(McpToolNaming.MaxNameLength);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_rejects_empty_ids(string canonicalId)
    {
        var act = () => McpToolNaming.Sanitize(canonicalId);

        act.Should().Throw<ArgumentException>();
    }
}
