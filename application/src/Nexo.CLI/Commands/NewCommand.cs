using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>
/// Scaffolds new Nexo extension artifacts.
/// </summary>
public sealed class NewCommand : Command
{
    public NewCommand()
        : base("new", "Scaffold Nexo extension artifacts.")
    {
        AddCommand(CreateBrickCommand());
    }

    private static Command CreateBrickCommand()
    {
        var nameArg = new Argument<string>("name", "Brick name, for example MyThing.");
        var outputOpt = new Option<string>(
            "--output",
            () => Environment.CurrentDirectory,
            "Directory where the brick solution folder should be created.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");

        var command = new Command("brick", "Scaffold a code-authored Nexo brick project and test project.")
        {
            nameArg,
            outputOpt,
            jsonOpt
        };

        command.SetHandler((InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var output = context.ParseResult.GetValueForOption(outputOpt) ?? Environment.CurrentDirectory;
            var json = context.ParseResult.GetValueForOption(jsonOpt);
            context.ExitCode = ExecuteBrick(name, output, json);
        });

        return command;
    }

    internal static int ExecuteBrick(string name, string outputDirectory, bool json)
    {
        var normalizedName = NormalizeBrickName(name);
        if (normalizedName is null)
        {
            WriteResult(false, "Brick name must be a valid C# identifier segment (letters, digits, underscore; cannot start with a digit).", null, json);
            return 1;
        }

        var repoRoot = FindRepoRoot();
        var templateRoot = Path.Combine(repoRoot, "samples", "templates", "brick");
        if (!Directory.Exists(templateRoot))
        {
            WriteResult(false, $"Brick template not found: {templateRoot}", null, json);
            return 1;
        }

        var root = Path.GetFullPath(outputDirectory);
        var projectRoot = Path.Combine(root, $"{normalizedName}Brick");
        var testsRoot = Path.Combine(root, $"{normalizedName}Brick.Tests");
        if ((Directory.Exists(projectRoot) && Directory.EnumerateFileSystemEntries(projectRoot).Any()) ||
            (Directory.Exists(testsRoot) && Directory.EnumerateFileSystemEntries(testsRoot).Any()))
        {
            WriteResult(false, $"Refusing to overwrite existing brick project directories under {root}.", null, json);
            return 1;
        }

        var replacements = BuildReplacements(normalizedName, projectRoot, repoRoot);
        CopyTemplate(templateRoot, root, replacements);

        var testProject = Path.Combine(testsRoot, $"{normalizedName}Brick.Tests.csproj");
        WriteResult(
            true,
            $"Scaffolded {normalizedName}Brick.",
            new
            {
                outputDirectory = root,
                project = Path.Combine(projectRoot, $"{normalizedName}Brick.csproj"),
                testProject,
                next = $"dotnet test \"{testProject}\""
            },
            json);
        return 0;
    }

    private static Dictionary<string, string> BuildReplacements(string brickName, string projectRoot, string repoRoot)
    {
        var coreDomainProject = Path.Combine(repoRoot, "src", "Nexo.Core.Domain", "Nexo.Core.Domain.csproj");
        var relativeCoreDomainProject = Path.GetRelativePath(projectRoot, coreDomainProject).Replace('\\', '/');
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["__BrickName__"] = brickName,
            ["__DisplayName__"] = $"{brickName} Brick",
            ["__BrickId__"] = ToBrickId(brickName),
            ["__Namespace__"] = $"{brickName}Brick",
            ["__NexoCoreDomainProjectReference__"] = relativeCoreDomainProject
        };
    }

    private static void CopyTemplate(string templateRoot, string outputRoot, IReadOnlyDictionary<string, string> replacements)
    {
        foreach (var sourcePath in Directory.GetFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(templateRoot, sourcePath);
            var targetRelative = ReplaceTokens(relative, replacements);
            var target = Path.Combine(outputRoot, targetRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            var text = File.ReadAllText(sourcePath);
            File.WriteAllText(target, ReplaceTokens(text, replacements));
        }
    }

    private static string ReplaceTokens(string value, IReadOnlyDictionary<string, string> replacements)
    {
        foreach (var (token, replacement) in replacements)
            value = value.Replace(token, replacement, StringComparison.Ordinal);
        return value;
    }

    private static string? NormalizeBrickName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var cleaned = new string(name.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrWhiteSpace(cleaned) || char.IsDigit(cleaned[0]))
            return null;

        return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
    }

    private static string ToBrickId(string name)
    {
        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('-');
            chars.Add(char.ToLowerInvariant(c));
        }
        return string.Concat(chars);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Nexo.sln")) &&
                Directory.Exists(Path.Combine(current.FullName, "samples", "templates", "brick")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static void WriteResult(bool ok, string summary, object? details, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok, summary, details }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine(ok ? "nexo new brick: ok" : "nexo new brick: failed");
        Console.WriteLine(summary);
        if (details is not null)
            Console.WriteLine(JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }));
    }
}
