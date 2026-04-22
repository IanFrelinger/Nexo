using Nexo.CLI.Commands;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

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
}
