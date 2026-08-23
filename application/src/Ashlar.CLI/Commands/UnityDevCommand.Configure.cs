using System.CommandLine;
using System.CommandLine.Invocation;

namespace Ashlar.CLI.Commands;

/// <summary>CLI command for unity dev.</summary>
public sealed partial class UnityDevCommand
{
    private Command CreateInitCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path where the Unity project should be created.") { IsRequired = true };
        var nameOpt = new Option<string>("--name", () => "AshlarForgeGame", "Project name (used for folder naming and Assembly Definitions).");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("init", "Scaffold a new Unity project structure ready for Ashlar Forge development.")
        {
            projectRootOpt, nameOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var name = ctx.ParseResult.GetValueForOption(nameOpt) ?? "AshlarForgeGame";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await Task.FromResult(ExecuteInit(projectRoot, name, json));
        });

        return cmd;
    }

    private Command CreateGenerateCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var systemOpt = new Option<string>("--system", "Description of the gameplay system to generate.") { IsRequired = true };
        var outputDirOpt = new Option<string>("--output-dir", () => DefaultOutputDir, "Relative path under project-root for generated scripts.");
        var testDirOpt = new Option<string>("--test-dir", () => DefaultTestDir, "Relative path under project-root for generated tests.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Show what would be generated without writing files.");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");
        var templateOpt = new Option<string?>("--template", () => null, "Path to a .template.json file for fixed/generated field control.");

        var cmd = new Command("generate", "Generate a new Unity gameplay system.")
        {
            projectRootOpt, systemOpt, outputDirOpt, testDirOpt, dryRunOpt, jsonOpt, templateOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var system = ctx.ParseResult.GetValueForOption(systemOpt)!;
            var outputDir = ctx.ParseResult.GetValueForOption(outputDirOpt) ?? DefaultOutputDir;
            var testDir = ctx.ParseResult.GetValueForOption(testDirOpt) ?? DefaultTestDir;
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var template = ctx.ParseResult.GetValueForOption(templateOpt);

            ctx.ExitCode = await ExecuteGenerateAsync(
                projectRoot, system, outputDir, testDir, dryRun, json,
                ct: ctx.GetCancellationToken(), templatePath: template).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateIterateCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var changeOpt = new Option<string>("--change", "Description of the change to make.") { IsRequired = true };
        var systemDirOpt = new Option<string>("--system-dir", "Relative folder under project-root containing the system to modify (e.g. Assets/Scripts/Weapons).") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("iterate", "Modify an existing Unity gameplay system.")
        {
            projectRootOpt, changeOpt, systemDirOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var change = ctx.ParseResult.GetValueForOption(changeOpt)!;
            var systemDir = ctx.ParseResult.GetValueForOption(systemDirOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteIterateAsync(
                projectRoot, change, systemDir, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateListCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("list", "List generated gameplay systems.")
        {
            projectRootOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = ExecuteList(projectRoot, json);
            await Task.CompletedTask;
        });

        return cmd;
    }

    private Command CreatePinCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var fileOpt = new Option<string>("--file", "Descriptor path relative to the project root.") { IsRequired = true };
        var fieldOpt = new Option<string>("--field", "Field name to pin or unpin.") { IsRequired = true };
        var valueOpt = new Option<string?>("--value", () => null, "Value to pin. Required unless --unpin is set.");
        var unpinOpt = new Option<bool>("--unpin", () => false, "Remove the pin for the given field.");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("pin", "Pin or unpin a field value on a descriptor to preserve it across regeneration.")
        {
            projectRootOpt, fileOpt, fieldOpt, valueOpt, unpinOpt, jsonOpt
        };

        cmd.SetHandler((InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var file = ctx.ParseResult.GetValueForOption(fileOpt)!;
            var field = ctx.ParseResult.GetValueForOption(fieldOpt)!;
            var value = ctx.ParseResult.GetValueForOption(valueOpt);
            var unpin = ctx.ParseResult.GetValueForOption(unpinOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = ExecutePin(projectRoot, file, field, value, unpin, json);
        });

        return cmd;
    }

    private Command CreateComposeCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var configOpt = new Option<string>("--config", () => ".ashlar/composition.json", "Path to the composition graph JSON file (relative to project root).");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("compose", "Run multi-system generation using a composition dependency graph.")
        {
            projectRootOpt, configOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var config = ctx.ParseResult.GetValueForOption(configOpt) ?? ".ashlar/composition.json";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteComposeAsync(
                projectRoot, config, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateAssetsCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var typeOpt = new Option<string>("--type", "Asset type to generate (material|prefab|scene|audio|animation|soundbank|animationset|ui|vfx|physics|input|network|ainavigation|build).") { IsRequired = true };
        var descriptionOpt = new Option<string>("--description", "Description of the asset to generate.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("assets", "Generate game asset definitions (JSON descriptors and C# loaders).")
        {
            projectRootOpt, typeOpt, descriptionOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var type = ctx.ParseResult.GetValueForOption(typeOpt)!;
            var description = ctx.ParseResult.GetValueForOption(descriptionOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteAssetsAsync(
                projectRoot, type, description, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateQaCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var maxIterOpt = new Option<int>("--max-iterations", () => 5, "Maximum compile/test/fix iterations.");
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");
        var unityPathOpt = new Option<string?>("--unity-path", () => null, "Path to the Unity editor executable. Auto-detected if omitted.");

        var cmd = new Command("qa", "Automated compile, test, and iterative-fix loop using the Unity editor CLI.")
        {
            projectRootOpt, maxIterOpt, jsonOpt, unityPathOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var maxIter = ctx.ParseResult.GetValueForOption(maxIterOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var unityPath = ctx.ParseResult.GetValueForOption(unityPathOpt);

            ctx.ExitCode = await ExecuteQaAsync(
                projectRoot, maxIter, json, unityPath,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

    private Command CreateFullstackCommand()
    {
        var projectRootOpt = new Option<string>("--project-root", "Path to the Unity project root.") { IsRequired = true };
        var gameDescOpt = new Option<string>("--game-description", "High-level description of the game to build.") { IsRequired = true };
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit machine-readable JSON output.");

        var cmd = new Command("fullstack", "Run the full pipeline: init → generate → assets → qa.")
        {
            projectRootOpt, gameDescOpt, jsonOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var projectRoot = ctx.ParseResult.GetValueForOption(projectRootOpt)!;
            var gameDesc = ctx.ParseResult.GetValueForOption(gameDescOpt)!;
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteFullstackAsync(
                projectRoot, gameDesc, json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });

        return cmd;
    }

}
