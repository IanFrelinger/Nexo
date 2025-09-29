using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.CLI;

enum ExitCode
{
    Ok = 0,
    ValidationFailed = 2,
    PolicyViolation = 3,
    UnexpectedError = 10
}

record CliEnvelope<T>(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("error")] string? Error
);

static class Program
{
    static int Main(string[] args)
    {
        var root = new RootCommand("Nexo command-line interface")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        // Global options
        var jsonOpt = new Option<bool>(
            name: "--format-json",
            description: "Emit machine-readable JSON (stderr still carries logs)."
        );

        root.AddGlobalOption(jsonOpt);

        // nexo analyze
        var analyzeCmd = new Command("analyze", "Run code/assembly analyzers and policies")
        {
            new Option<DirectoryInfo>(
                name: "--path",
                description: "Root path to analyze (defaults to current)",
                getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory)
            )
        };
        analyzeCmd.SetHandler((DirectoryInfo path, bool json) =>
        {
            try
            {
                // TODO: wire to your real analyzers/services
                var violations = new string[] { /* fill from services */ };
                if (violations.Length == 0)
                {
                    Emit<object>(json, new { message = "No violations" }, null, ExitCode.Ok);
                }
                else
                {
                    Emit<object>(json, null, $"Found {violations.Length} violation(s).", ExitCode.ValidationFailed);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Emit<object>(json, null, $"Policy violation: {ex.Message}", ExitCode.PolicyViolation);
            }
            catch (Exception ex)
            {
                Emit<object>(json, null, ex.Message, ExitCode.UnexpectedError);
            }
        }, analyzeCmd.Options[0] as Option<DirectoryInfo> ?? throw new InvalidOperationException(), jsonOpt);

        // nexo validate
        var validateCmd = new Command("validate", "Run architecture tests/contract checks quickly")
        {
            new Option<string?>("--filter", "Optional test filter (Category/Trait)")
        };
        validateCmd.SetHandler((string? filter, bool json) =>
        {
            try
            {
                // TODO: route to your test runner facade or lightweight validations
                var passed = true; // flip when failures
                if (passed) Emit<object>(json, new { message = "Validation passed" }, null, ExitCode.Ok);
                else Emit<object>(json, null, "Validation failed", ExitCode.ValidationFailed);
            }
            catch (Exception ex)
            {
                Emit<object>(json, null, ex.Message, ExitCode.UnexpectedError);
            }
        }, validateCmd.Options[0] as Option<string?> ?? throw new InvalidOperationException(), jsonOpt);

        // nexo agent run
        var agentCmd = new Command("agent", "Run an agent action")
        {
            new Option<string>("--name", "Agent name") { IsRequired = true },
            new Option<FileInfo?>("--input", "Optional input file")
        };
        agentCmd.SetHandler((string name, FileInfo? input, bool json) =>
        {
            try
            {
                // TODO: call your AgentFactory and execute
                var result = new { agent = name, ran = true };
                Emit<object>(json, result, null, ExitCode.Ok);
            }
            catch (TimeoutException ex)
            {
                Emit<object>(json, null, $"Timeout: {ex.Message}", ExitCode.ValidationFailed);
            }
            catch (Exception ex)
            {
                Emit<object>(json, null, ex.Message, ExitCode.UnexpectedError);
            }
        },
        agentCmd.Options[0] as Option<string> ?? throw new InvalidOperationException(),
        agentCmd.Options[1] as Option<FileInfo?> ?? throw new InvalidOperationException(),
        jsonOpt);

        root.AddCommand(analyzeCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(agentCmd);

        return root.Invoke(args);

        static void Emit<T>(bool json, T? data, string? error, ExitCode code)
        {
            if (json)
            {
                var env = new CliEnvelope<T>(code == ExitCode.Ok, data, error);
                var payload = JsonSerializer.Serialize(env, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                Console.Out.WriteLine(payload);
            }
            else
            {
                if (code == ExitCode.Ok) Console.Out.WriteLine(data is null ? "OK" : data!.ToString());
                else Console.Error.WriteLine(error);
            }

            Environment.Exit((int)code);
        }
    }
}