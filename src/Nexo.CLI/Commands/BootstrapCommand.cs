using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;

namespace Nexo.CLI.Commands;

public sealed class BootstrapCommand : Command
{
    public BootstrapCommand() : base("bootstrap", "Linux-first environment bootstrap (check/install dependencies)")
    {
        AddAlias("doctor");

        var includeOptionalOpt = new Option<bool>(
            "--include-optional",
            () => false,
            "Include optional dependencies (docker, ollama) in checks/install plan.");
        var profileOpt = new Option<string>(
            "--profile",
            () => "demo",
            "Bootstrap profile: demo | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        var yesOpt = new Option<bool>("--yes", () => false, "Auto-approve install plan.");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Show install plan without executing commands.");

        var checkCmd = new Command("check", "Check local machine readiness for the demo profile.");
        checkCmd.AddOption(includeOptionalOpt);
        checkCmd.AddOption(profileOpt);
        checkCmd.AddOption(jsonOpt);
        checkCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            Environment.ExitCode = await RunCheckAsync(profile, includeOptional, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(checkCmd);

        var applyCmd = new Command("apply", "Install missing dependencies for the demo profile.");
        applyCmd.AddOption(includeOptionalOpt);
        applyCmd.AddOption(profileOpt);
        applyCmd.AddOption(jsonOpt);
        applyCmd.AddOption(yesOpt);
        applyCmd.AddOption(dryRunOpt);
        applyCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var yes = ctx.ParseResult.GetValueForOption(yesOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            Environment.ExitCode = await RunApplyAsync(profile, includeOptional, yes, dryRun, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(applyCmd);

        // Default command behavior: `nexo bootstrap` == `nexo bootstrap check`
        AddOption(includeOptionalOpt);
        AddOption(profileOpt);
        AddOption(jsonOpt);
        this.SetHandler(async (InvocationContext ctx) =>
        {
            var includeOptional = ctx.ParseResult.GetValueForOption(includeOptionalOpt);
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "demo";
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            Environment.ExitCode = await RunCheckAsync(profile, includeOptional, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
    }

    internal static async Task<int> RunCheckAsync(string profile, bool includeOptional, bool json, CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        BootstrapRuntime.RenderAssessment(assessment, json);
        return 0;
    }

    internal static async Task<int> RunApplyAsync(string profile, bool includeOptional, bool yes, bool dryRun, bool json, CancellationToken ct)
    {
        var assessment = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        if (!assessment.Supported)
        {
            BootstrapRuntime.RenderAssessment(assessment, json);
            return 1;
        }

        var plan = BootstrapRuntime.BuildInstallPlan(assessment);
        if (plan.Count == 0)
        {
            BootstrapRuntime.RenderAssessment(assessment, json);
            if (!json)
                Console.WriteLine("Bootstrap apply: nothing to install.");
            return 0;
        }

        if (!yes && !dryRun)
        {
            Console.WriteLine("Install plan:");
            foreach (var dep in plan)
                Console.WriteLine($"  - {dep.DisplayName}: {dep.InstallCommand}");

            Console.Write("Proceed with installation? [y/N]: ");
            var answer = Console.ReadLine();
            if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelled.");
                return 130;
            }
        }

        if (dryRun)
        {
            if (json)
            {
                var payload = new
                {
                    ok = true,
                    dryRun = true,
                    steps = plan.Select(p => new { id = p.Id, name = p.DisplayName, install = p.InstallCommand }).ToArray(),
                };
                Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("Dry run install plan:");
                foreach (var dep in plan)
                    Console.WriteLine($"  - {dep.DisplayName}: {dep.InstallCommand}");
            }
            return 0;
        }

        var failures = new List<string>();
        foreach (var dep in plan)
        {
            Console.WriteLine($"Installing {dep.DisplayName}...");
            var exitCode = await BootstrapRuntime.RunShellStreamingAsync(dep.InstallCommand, ct).ConfigureAwait(false);
            if (exitCode != 0)
                failures.Add($"{dep.DisplayName} (exit {exitCode})");
        }

        var post = await BootstrapRuntime.AssessDemoAsync(profile, includeOptional, ct).ConfigureAwait(false);
        var success = !post.MissingRequired.Any() && failures.Count == 0;

        if (json)
        {
            var payload = new
            {
                ok = success,
                installed = plan.Select(p => p.Id).ToArray(),
                failures,
                missingRequiredAfterApply = post.MissingRequired.Select(m => m.Id).ToArray(),
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            if (failures.Count > 0)
                Console.WriteLine($"Install failures: {string.Join(", ", failures)}");
            if (post.MissingRequired.Any())
                Console.WriteLine($"Still missing required dependencies: {string.Join(", ", post.MissingRequired.Select(m => m.DisplayName))}");
            Console.WriteLine(success ? "Bootstrap apply completed successfully." : "Bootstrap apply completed with errors.");
        }

        return success ? 0 : 1;
    }
}

internal static class BootstrapRuntime
{
    private static readonly IReadOnlyList<BootstrapDependencySpec> LinuxDemoDependencies =
    [
        new(
            "git",
            "Git",
            "command -v git",
            "sudo apt-get update && sudo apt-get install -y git",
            true,
            false),
        new(
            "curl",
            "curl",
            "command -v curl",
            "sudo apt-get update && sudo apt-get install -y curl",
            true,
            false),
        new(
            "dotnet",
            ".NET SDK",
            "command -v dotnet",
            "sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0",
            true,
            false),
        new(
            "zstd",
            "zstd",
            "command -v zstd",
            "sudo apt-get update && sudo apt-get install -y zstd",
            false,
            true),
        new(
            "ollama",
            "Ollama",
            "command -v ollama",
            "curl -fsSL https://ollama.com/install.sh | sh",
            false,
            true),
        new(
            "docker",
            "Docker",
            "command -v docker",
            "sudo apt-get update && sudo apt-get install -y docker.io",
            false,
            true),
    ];

    private static readonly IReadOnlyList<BootstrapDependencySpec> MacDemoDependencies =
    [
        new(
            "brew",
            "Homebrew",
            "command -v brew",
            "/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"",
            true,
            false),
        new(
            "git",
            "Git",
            "command -v git",
            "brew install git",
            true,
            false),
        new(
            "dotnet",
            ".NET SDK",
            "command -v dotnet",
            "brew install --cask dotnet-sdk",
            true,
            false),
        new(
            "ollama",
            "Ollama",
            "command -v ollama",
            "brew install ollama",
            false,
            true),
        new(
            "docker",
            "Docker Desktop",
            "command -v docker",
            "brew install --cask docker",
            false,
            true),
    ];

    public static async Task<BootstrapAssessment> AssessDemoAsync(string profile, bool includeOptional, CancellationToken ct)
    {
        var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var (_, supported, reason, deps) = ResolveProfile();
        if (!supported)
        {
            return new BootstrapAssessment(
                NormalizeBootstrapProfile(profile),
                os,
                false,
                reason,
                Array.Empty<BootstrapDependencyStatus>());
        }

        var normalizedBootstrapProfile = NormalizeBootstrapProfile(profile);
        var statuses = new List<BootstrapDependencyStatus>();
        foreach (var spec in deps)
        {
            var (exitCode, _, stderr) = await RunShellCaptureAsync(spec.ProbeCommand, ct).ConfigureAwait(false);
            var required = IsRequiredForBootstrapProfile(normalizedBootstrapProfile, spec, includeOptional);
            statuses.Add(new BootstrapDependencyStatus(
                spec.Id,
                spec.DisplayName,
                exitCode == 0,
                required,
                !required,
                spec.InstallCommand,
                exitCode == 0 ? null : stderr.Trim()));
        }

        return new BootstrapAssessment(
            normalizedBootstrapProfile,
            os,
            true,
            null,
            statuses);
    }

    private static (string Profile, bool Supported, string? Reason, IReadOnlyList<BootstrapDependencySpec> Deps) ResolveProfile()
    {
        if (OperatingSystem.IsLinux())
            return ("linux-demo", true, null, LinuxDemoDependencies);
        if (OperatingSystem.IsMacOS())
            return ("mac-demo", true, null, MacDemoDependencies);
        return ("unsupported", false, "Bootstrap currently supports Linux and macOS.", Array.Empty<BootstrapDependencySpec>());
    }

    public static IReadOnlyList<BootstrapDependencyStatus> BuildInstallPlan(BootstrapAssessment assessment)
    {
        return assessment.Dependencies
            .Where(d => !d.Installed && d.Required)
            .ToList();
    }

    public static void RenderAssessment(BootstrapAssessment assessment, bool json)
    {
        if (json)
        {
            var payload = new
            {
                profile = assessment.Profile,
                os = assessment.OsDescription,
                supported = assessment.Supported,
                reason = assessment.Reason,
                dependencies = assessment.Dependencies.Select(d => new
                {
                    id = d.Id,
                    name = d.DisplayName,
                    installed = d.Installed,
                    required = d.Required,
                    optional = d.Optional,
                    install = d.InstallCommand,
                    probeError = d.ProbeError,
                }).ToArray(),
                missingRequired = assessment.MissingRequired.Select(d => d.Id).ToArray(),
                installPlan = BuildInstallPlan(assessment).Select(d => d.Id).ToArray(),
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"Bootstrap profile: {assessment.Profile}");
        Console.WriteLine($"Host OS: {assessment.OsDescription}");
        if (!assessment.Supported)
        {
            Console.WriteLine(assessment.Reason ?? "Unsupported host.");
            return;
        }

        Console.WriteLine("Dependency status:");
        foreach (var dep in assessment.Dependencies)
        {
            var label = dep.Installed ? "OK" : "MISSING";
            var requiredLabel = dep.Required ? "required" : "optional";
            Console.WriteLine($"  - [{label}] {dep.DisplayName} ({requiredLabel})");
            if (!dep.Installed)
                Console.WriteLine($"      install: {dep.InstallCommand}");
        }

        var plan = BuildInstallPlan(assessment);
        if (plan.Count == 0)
            Console.WriteLine("Install plan: no action needed.");
        else
            Console.WriteLine($"Install plan: {string.Join(", ", plan.Select(p => p.DisplayName))}");
    }

    private static string NormalizeBootstrapProfile(string? profile)
    {
        var normalized = (profile ?? "demo").Trim().ToLowerInvariant();
        return normalized switch
        {
            "self-extend-functional" => "self-extend-functional",
            "self-extend-aesthetic" => "self-extend-aesthetic",
            "self-extend-visual" => "self-extend-visual",
            _ => "demo"
        };
    }

    private static bool IsRequiredForBootstrapProfile(string profile, BootstrapDependencySpec spec, bool includeOptional)
    {
        if (profile == "self-extend-visual")
        {
            if (spec.Id is "docker" or "ollama" or "zstd")
                return true;
        }

        if (profile == "self-extend-functional")
        {
            return spec.Required;
        }

        if (profile == "self-extend-aesthetic")
        {
            if (spec.Id is "docker")
                return true;
            return spec.Required;
        }

        return spec.Required || (includeOptional && spec.Optional);
    }

    public static async Task<int> RunShellStreamingAsync(string command, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(command);

        using var process = Process.Start(psi);
        if (process == null)
            return 1;

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunShellCaptureAsync(string command, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(command);

        using var process = Process.Start(psi);
        if (process == null)
            return (1, string.Empty, "Failed to start process.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stdErrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdOutTask.ConfigureAwait(false);
        var stderr = await stdErrTask.ConfigureAwait(false);
        return (process.ExitCode, stdout, stderr);
    }
}

internal sealed record BootstrapDependencySpec(
    string Id,
    string DisplayName,
    string ProbeCommand,
    string InstallCommand,
    bool Required,
    bool Optional);

internal sealed record BootstrapDependencyStatus(
    string Id,
    string DisplayName,
    bool Installed,
    bool Required,
    bool Optional,
    string InstallCommand,
    string? ProbeError);

internal sealed record BootstrapAssessment(
    string Profile,
    string OsDescription,
    bool Supported,
    string? Reason,
    IReadOnlyList<BootstrapDependencyStatus> Dependencies)
{
    public IEnumerable<BootstrapDependencyStatus> MissingRequired =>
        Dependencies.Where(d => d.Required && !d.Installed);
}
