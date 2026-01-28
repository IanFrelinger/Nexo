using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Xml;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for aggregating test results and artifacts.
/// Replaces aggregate-junit.sh and aggregate-junit-cs.sh scripts.
/// </summary>
public class AggregateCommand : Command
{
    public AggregateCommand() : base("aggregate", "Aggregate test results and artifacts")
    {
        // nexo aggregate junit
        var junitCmd = new Command("junit", "Aggregate multiple JUnit XML files into a single report");
        
        var outputOption = new Option<FileInfo>(
            "--output",
            () => new FileInfo("playmode-review-aggregate.junit.xml"),
            "Output JUnit XML file path"
        );

        var inputOption = new Option<FileInfo[]>(
            "--input",
            () => Array.Empty<FileInfo>(),
            "Input JUnit XML files to aggregate"
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        var patternOption = new Option<string>(
            "--pattern",
            () => "playmode-smoke.junit.xml",
            "File pattern to search for in Artifacts directory (used if --input is not specified)"
        );

        junitCmd.AddOption(outputOption);
        junitCmd.AddOption(inputOption);
        junitCmd.AddOption(patternOption);

        junitCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var output = ctx.ParseResult.GetValueForOption(outputOption) ?? new FileInfo("playmode-review-aggregate.junit.xml");
            var inputs = ctx.ParseResult.GetValueForOption(inputOption) ?? Array.Empty<FileInfo>();
            var pattern = ctx.ParseResult.GetValueForOption(patternOption) ?? "playmode-smoke.junit.xml";
            
            // Get global options from root command
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            
            await AggregateJUnitAsync(output, inputs, pattern, json, verbose);
        });

        AddCommand(junitCmd);
    }

    private static async Task AggregateJUnitAsync(
        FileInfo output,
        FileInfo[] inputs,
        string pattern,
        bool json,
        bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!json && console != null)
        {
            console.WriteHeader("📊 Aggregating JUnit Results");
            console.WriteLine();
        }

        try
        {
            var rootDir = DiscoverProjectRoot();
            if (rootDir == null)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Could not find project root directory" }));
                }
                else if (console != null)
                {
                    console.WriteError("Could not find project root directory");
                }
                Environment.ExitCode = 1;
                return;
            }

            // Get input files
            var inputFiles = new List<string>();

            if (inputs.Length > 0)
            {
                inputFiles.AddRange(inputs.Where(f => f.Exists).Select(f => f.FullName));
            }
            else
            {
                // Search for files matching pattern in Artifacts directory
                var artifactsDir = Path.Combine(rootDir, "Artifacts");
                if (Directory.Exists(artifactsDir))
                {
                    inputFiles.AddRange(Directory.GetFiles(artifactsDir, pattern, SearchOption.AllDirectories));
                }
            }

            if (inputFiles.Count == 0)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "No JUnit files found to aggregate" }));
                }
                else if (console != null)
                {
                    console.WriteError("No JUnit files found to aggregate");
                }
                Environment.ExitCode = 1;
                return;
            }

            if (!json && console != null)
            {
                console.WritePair("Input Files", inputFiles.Count.ToString());
                console.WritePair("Output File", output.FullName);
                console.WriteLine();
            }

            var totalTests = 0;
            var totalFailures = 0;
            var totalTime = 0.0;
            var testCases = new List<string>();

            foreach (var file in inputFiles)
            {
                if (!File.Exists(file))
                {
                    if (verbose && !json && console != null)
                    {
                        console.WriteWarning($"File not found: {file}");
                    }
                    continue;
                }

                try
                {
                    var doc = new XmlDocument();
                    doc.Load(file);

                    var root = doc.DocumentElement;
                    if (root == null)
                    {
                        if (verbose && !json && console != null)
                        {
                            console.WriteWarning($"Invalid XML in {file}");
                        }
                        continue;
                    }

                    var tests = GetIntAttribute(root, "tests");
                    var failures = GetIntAttribute(root, "failures");
                    var time = GetDoubleAttribute(root, "time");

                    totalTests += tests;
                    totalFailures += failures;
                    totalTime += time;

                    // Extract test cases
                    var testCaseNodes = root.SelectNodes("//testcase");
                    if (testCaseNodes != null)
                    {
                        foreach (XmlNode testCase in testCaseNodes)
                        {
                            testCases.Add(testCase.OuterXml);
                        }
                    }

                    if (verbose && !json && console != null)
                    {
                        console.WriteLine($"  Processed: {Path.GetFileName(file)} ({tests} tests, {failures} failures)");
                    }
                }
                catch (Exception ex)
                {
                    if (json)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new { warning = $"Could not process {file}: {ex.Message}" }));
                    }
                    else if (console != null)
                    {
                        console.WriteWarning($"Could not process {file}: {ex.Message}");
                    }
                }
            }

            // Create aggregated XML
            var aggregatedXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""ReviewAggregate"" tests=""{totalTests}"" failures=""{totalFailures}"" time=""{totalTime:F1}"">
{string.Join("\n", testCases)}
</testsuite>";

            // Ensure output directory exists
            if (output.Directory != null && !output.Directory.Exists)
            {
                output.Directory.Create();
            }

            await File.WriteAllTextAsync(output.FullName, aggregatedXml);

            if (!json && console != null)
            {
                console.WriteLine();
                console.WriteSuccess($"✅ Wrote {output.FullName} (tests={totalTests}, failures={totalFailures})");
            }
            else if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    outputFile = output.FullName,
                    tests = totalTests,
                    failures = totalFailures,
                    time = totalTime,
                    inputFiles = inputFiles.Count
                }));
            }

            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (console != null)
            {
                console.WriteError($"Error: {ex.Message}");
            }
            Environment.ExitCode = 1;
        }
    }

    private static int GetIntAttribute(XmlElement? element, string attributeName)
    {
        if (element?.GetAttribute(attributeName) is { } value && int.TryParse(value, out var result))
            return result;
        return 0;
    }

    private static double GetDoubleAttribute(XmlElement? element, string attributeName)
    {
        if (element?.GetAttribute(attributeName) is { } value && double.TryParse(value, out var result))
            return result;
        return 0.0;
    }

    private static string? DiscoverProjectRoot()
    {
        var current = Environment.CurrentDirectory;
        var dir = new DirectoryInfo(current);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return null;
    }
}
