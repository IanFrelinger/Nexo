using System.CommandLine;

namespace Ashlar.Commercial.MeshDirector;

/// <summary>Program.</summary>
internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        var root = new RootCommand("Ashlar commercial mesh director CLI");
        root.AddCommand(new MeshDirectorCommand());
        return root.InvokeAsync(args);
    }
}
