using System.Text.Json;
using Ashlar.BackgroundAgents.HostRunners;

namespace Ashlar.CLI.Commands.Unity;
/// <summary>Handles qa requests.</summary>
internal sealed class QaHandler(Func<SelfExtendRunnerAdapter> runnerFactory)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string projectRoot,
        int maxIterations,
        bool json,
        string? unityPath,
        CancellationToken ct)
    {
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!UnityDevCommand.ValidateProjectRoot(fullProjectRoot, json))
            return 1;

        var unity = unityPath ?? UnityDevCommand.FindUnityEditor();
        if (unity == null)
        {
            UnityDevCommand.WriteError("Unity editor not found. Provide --unity-path or ensure Unity is on PATH.", json);
            return 1;
        }

        UnityDevCommand.SetPathAllowlist(fullProjectRoot);
        var iterations = new List<object>();

        for (int i = 1; i <= maxIterations; i++)
        {
            if (!json) Console.WriteLine($"QA iteration {i}/{maxIterations}: compiling...");

            var (buildExit, buildOutput) = await UnityDevCommand.RunUnityCommand(
                unity, fullProjectRoot,
                "-batchmode -nographics -logFile - -quit",
                ct).ConfigureAwait(false);

            if (buildExit != 0)
            {
                if (!json) Console.WriteLine($"  Compilation failed (exit {buildExit}). Requesting LLM fix...");

                var fixPrompt = $@"{UnityDevCommand.UnitySystemPrompt}

The Unity project at {fullProjectRoot} failed to compile.
Build output:
{UnityDevCommand.Truncate(buildOutput, 4000)}

Analyse the errors and generate corrected files. Each file must start with a // FILE: marker.";

                var runner = runnerFactory();
                var fixResult = await runner.RunAsync(fullProjectRoot, fixPrompt, "unity-dev-qa-fix", ct).ConfigureAwait(false);

                var fixedFiles = UnityDevCommand.ParseFiles(fixResult.Summary);
                foreach (var (rp, content) in fixedFiles)
                {
                    var fp = Path.Combine(fullProjectRoot, rp);
                    var d = Path.GetDirectoryName(fp);
                    if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d);
                    await File.WriteAllTextAsync(fp, content, ct).ConfigureAwait(false);
                }

                iterations.Add(new { iteration = i, phase = "build", passed = false, filesFixed = fixedFiles.Count });
                continue;
            }

            if (!json) Console.WriteLine("  Compilation succeeded. Running EditMode tests...");

            var testResultPath = Path.Combine(fullProjectRoot, "TestResults", $"qa-iter-{i}.xml");
            var testDir = Path.GetDirectoryName(testResultPath);
            if (!string.IsNullOrEmpty(testDir)) Directory.CreateDirectory(testDir);

            var (testExit, testOutput) = await UnityDevCommand.RunUnityCommand(
                unity, fullProjectRoot,
                $"-batchmode -nographics -runTests -testPlatform EditMode -testResults \"{testResultPath}\" -logFile - -quit",
                ct).ConfigureAwait(false);

            if (testExit == 0)
            {
                if (!json) Console.WriteLine("  All tests passed!");
                iterations.Add(new { iteration = i, phase = "test", passed = true, filesFixed = 0 });
                /// <summary>Write result.</summary>
                WriteResult(iterations, i, true, json);
                return 0;
            }

            if (!json) Console.WriteLine($"  Tests failed (exit {testExit}). Requesting LLM fix...");

            var testFixPrompt = $@"{UnityDevCommand.UnitySystemPrompt}

The Unity project at {fullProjectRoot} compiled successfully but EditMode tests failed.
Test output:
{UnityDevCommand.Truncate(testOutput, 4000)}

Analyse the test failures and generate corrected files. Each file must start with a // FILE: marker.";

            var testRunner = runnerFactory();
            var testFixResult = await testRunner.RunAsync(fullProjectRoot, testFixPrompt, "unity-dev-qa-fix", ct).ConfigureAwait(false);

            var testFixedFiles = UnityDevCommand.ParseFiles(testFixResult.Summary);
            foreach (var (rp, content) in testFixedFiles)
            {
                var fp = Path.Combine(fullProjectRoot, rp);
                var d = Path.GetDirectoryName(fp);
                if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d);
                await File.WriteAllTextAsync(fp, content, ct).ConfigureAwait(false);
            }

            iterations.Add(new { iteration = i, phase = "test", passed = false, filesFixed = testFixedFiles.Count });
        }

        if (!json) Console.WriteLine($"QA: max iterations ({maxIterations}) reached without all tests passing.");
        /// <summary>Write result.</summary>
        WriteResult(iterations, maxIterations, false, json);
        return 1;
    }

    private static void WriteResult(List<object> iterations, int totalIterations, bool allPassed, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = allPassed,
                action = "qa",
                totalIterations,
                allPassed,
                iterations
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unity-dev qa: {(allPassed ? "ok" : "failed")}");
            Console.WriteLine($"Iterations used: {totalIterations}");
            Console.WriteLine($"All tests passed: {allPassed}");
        }
    }
}
