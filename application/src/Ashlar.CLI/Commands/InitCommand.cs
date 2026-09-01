using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.Manifest;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar init &lt;name&gt;</c> — scaffold a new project: the manifest agents may propose
/// changes to, and the operator-owned policy that constrains what it may become.
///
/// <para>Thin by design: every decision about what the documents contain, and whether they
/// are valid, lives in <see cref="ProjectScaffold"/> where it is testable without booting
/// the CLI. This class only decides where files go and refuses to overwrite.</para>
/// </summary>
public sealed class InitCommand : Command
{
    /// <summary>Creates a new InitCommand instance.</summary>
    public InitCommand() : base("init", "Scaffold a new project: ashlar.yaml and its operator-owned policy.")
    {
        var nameArg = new Argument<string>("name", "Project name (letters, digits, hyphens; e.g. invoice-triage).");
        var pathOpt = new Option<DirectoryInfo>(
            name: "--path",
            description: "Directory to scaffold into (defaults to current).",
            getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory));

        AddArgument(nameArg);
        AddOption(pathOpt);

        this.SetHandler((InvocationContext ctx) =>
        {
            var name = ctx.ParseResult.GetValueForArgument(nameArg);
            var path = ctx.ParseResult.GetValueForOption(pathOpt)!;
            ctx.ExitCode = Execute(name, path);
        });
    }

    private static int Execute(string name, DirectoryInfo directory)
    {
        if (!ProjectScaffold.TryScaffold(name, out var manifestYaml, out var policyYaml, out var reason))
        {
            Console.Error.WriteLine(reason);
            return 1;
        }

        // A --path that names an existing FILE (not a directory) would make Directory.CreateDirectory
        // below throw an IOException, spraying a stack trace that leaks source paths. Refuse it
        // legibly, the same shape as every other rejection here.
        if (File.Exists(directory.FullName))
        {
            Console.Error.WriteLine($"REJECTED: --path '{directory.FullName}' is not a directory.");
            return 1;
        }

        var manifestPath = Path.Combine(directory.FullName, "ashlar.yaml");
        var policyPath = Path.Combine(directory.FullName, "ashlar.policy.yaml");

        // Refuse to overwrite: an existing project's policy is exactly the file a scaffold
        // must never quietly replace.
        var existing = new[] { manifestPath, policyPath }.Where(File.Exists).ToList();
        if (existing.Count > 0)
        {
            Console.Error.WriteLine(
                "REJECTED: refusing to overwrite existing project files: "
                + string.Join(", ", existing.Select(Path.GetFileName))
                + ". Remove them first if you really mean to start over.");
            return 1;
        }

        // Any remaining filesystem failure (a file sitting where a parent directory must go, a
        // read-only or permission-denied target) becomes a legible rejection rather than an
        // unhandled IOException/UnauthorizedAccessException stack trace.
        try
        {
            Directory.CreateDirectory(directory.FullName);
            File.WriteAllText(manifestPath, manifestYaml);
            File.WriteAllText(policyPath, policyYaml);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"REJECTED: could not write project files under '{directory.FullName}': {ex.Message}");
            return 1;
        }

        Console.WriteLine($"  ashlar.yaml           project contract for '{name}'");
        Console.WriteLine("  ashlar.policy.yaml    sandbox: .  ·  self-extend: sealed");
        Console.WriteLine();
        Console.WriteLine("  review the policy before you deploy. it is the only file");
        Console.WriteLine("  the running app can never change.");
        return 0;
    }
}
