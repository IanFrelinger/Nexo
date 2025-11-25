using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Runtime;
using Nexo.Tools.Assembly;
using Nexo.Policies;
// using Nexo.Demo.CLI.Agents; // Removed as part of Director Studio cleanup

namespace Nexo.Demo.CLI;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.SequenceEqual(new[] { "--help" }))
        {
            Console.WriteLine("Usage: nexo-demo demo run --input <path> --out <dir> --seed <int>");
            return Task.FromResult(0);
        }

        if (args.Length >= 2 && args[0] == "demo" && args[1] == "run")
        {
            // Director agents removed as part of Director Studio cleanup
            Console.Error.WriteLine("Director agents have been removed. This demo program is no longer functional.");
            return Task.FromResult(1);
        }

        Console.Error.WriteLine("Unknown command. Use: demo run --input <path> --out <dir> --seed <int>");
        return Task.FromResult(1);
    }
}
