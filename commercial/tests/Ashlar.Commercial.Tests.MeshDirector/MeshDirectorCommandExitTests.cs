using System.CommandLine;
using Ashlar.Commercial.MeshDirector;
using Xunit;

namespace Ashlar.Commercial.Tests.MeshDirector;

/// <summary>
/// Director handlers used to set <c>Environment.ExitCode</c>, which System.CommandLine
/// overwrites back to 0 after the handler returns. Failures must return a non-zero
/// <c>Task&lt;int&gt;</c> (or set <c>ctx.ExitCode</c>) so operator scripts fail closed.
/// </summary>
public sealed class MeshDirectorCommandExitTests
{
    [Fact]
    public async Task Health_missing_base_url_exits_nonzero()
    {
        var previous = Environment.GetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv);
        Environment.SetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv, null);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshDirectorCommand());
            var exitCode = await root.InvokeAsync("director health");
            Assert.NotEqual(0, exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv, previous);
        }
    }

    [Fact]
    public async Task Health_refused_connection_exits_nonzero()
    {
        var previous = Environment.GetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv);
        Environment.SetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv, "http://127.0.0.1:59321");
        try
        {
            var root = new RootCommand();
            root.AddCommand(new MeshDirectorCommand());
            var exitCode = await root.InvokeAsync("director health --timeout-seconds 5");
            Assert.NotEqual(0, exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeshDirectorCommand.DirectorBaseUrlEnv, previous);
        }
    }
}
