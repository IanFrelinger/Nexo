using System.CommandLine;

namespace Nexo.Commercial.MeshDirector;

internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        var root = new RootCommand("Nexo commercial mesh director CLI");
        root.AddCommand(new MeshDirectorCommand());
        return root.InvokeAsync(args);
    }
}
