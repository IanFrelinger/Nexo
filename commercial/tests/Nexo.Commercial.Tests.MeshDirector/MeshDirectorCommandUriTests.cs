using Nexo.Commercial.MeshDirector;
using Xunit;

namespace Nexo.Commercial.Tests.MeshDirector;

public sealed class MeshDirectorCommandUriTests
{
    [Theory]
    [InlineData("https://hub/", "/api/mesh/tasks", "https://hub/api/mesh/tasks")]
    [InlineData("https://hub", "api/mesh/tasks", "https://hub/api/mesh/tasks")]
    public void BuildRequestUri_normalizes_base_and_path(string baseUrl, string path, string expected)
    {
        var uri = MeshDirectorCommand.BuildRequestUri(baseUrl, path);
        Assert.Equal(expected, uri.ToString());
    }

    [Fact]
    public void BuildFleetNodePath_escapes_peer_id()
    {
        var path = MeshDirectorCommand.BuildFleetNodePath("peer/a", "admit");
        Assert.Equal("/api/mesh/fleet/nodes/peer%2Fa/admit", path);
    }
}
