using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for generating reports from JSON data.
/// Replaces review-summary-md.sh script.
/// </summary>
public class ReportCommand : Command
{
    public ReportCommand() : base("report", "Generate reports from JSON data")
    {
        // nexo report markdown
        var markdownCmd = new Command("markdown", "Generate markdown report from JSON");
        var inputOpt = new Option<FileInfo>(
            "--input",
            () => new FileInfo("review_summary.json"),
            "Input JSON file"
        );
        var outputOpt = new Option<FileInfo>(
            "--output",
            () => new FileInfo("REVIEW_SUMMARY.md"),
            "Output markdown file"
        );

        markdownCmd.AddOption(inputOpt);
        markdownCmd.AddOption(outputOpt);

        markdownCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var input = ctx.ParseResult.GetValueForOption(inputOpt) ?? new FileInfo("review_summary.json");
            var output = ctx.ParseResult.GetValueForOption(outputOpt) ?? new FileInfo("REVIEW_SUMMARY.md");
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            await GenerateMarkdownAsync(input, output, json, verbose);
        });

        AddCommand(markdownCmd);
    }

    private static async Task GenerateMarkdownAsync(
        FileInfo input,
        FileInfo output,
        bool json,
        bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        try
        {
            if (!input.Exists || input.Length == 0)
            {
                var content = "# Review Summary\n\n_No review_summary.json found._\n";
                await File.WriteAllTextAsync(output.FullName, content);

                if (!json && console != null)
                {
                    console.WriteWarning($"Input file not found or empty: {input.FullName}");
                    console.WriteLine($"Created empty report: {output.FullName}");
                }
                else if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = "Created empty report",
                        output = output.FullName
                    }));
                }
                Environment.ExitCode = 0;
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(input.FullName);
            var jsonNode = JsonNode.Parse(jsonContent);

            var markdown = new System.Text.StringBuilder();
            markdown.AppendLine("# Review Summary");
            markdown.AppendLine();
            markdown.AppendLine("```json");
            markdown.AppendLine(jsonContent);
            markdown.AppendLine("```");
            markdown.AppendLine();

            // Extract quick verdict if available
            if (jsonNode != null)
            {
                var seeds = jsonNode["seeds"]?.AsArray();
                if (seeds != null && seeds.Count > 0)
                {
                    var allPlayable = true;
                    var anyFallbacks = false;

                    foreach (var seed in seeds)
                    {
                        if (seed?["status"]?.GetValue<string>() != "playable")
                        {
                            allPlayable = false;
                        }
                        if (seed?["fallbacksUsed"]?.GetValue<bool>() == true)
                        {
                            anyFallbacks = true;
                        }
                    }

                    markdown.AppendLine($"- **Playable across seeds:** {(allPlayable ? "true" : "false")}");
                    markdown.AppendLine($"- **Fallbacks used:** {(anyFallbacks ? "true" : "false")}");
                    markdown.AppendLine();
                }
            }

            // Ensure output directory exists
            if (output.Directory != null && !output.Directory.Exists)
            {
                output.Directory.Create();
            }

            await File.WriteAllTextAsync(output.FullName, markdown.ToString());

            if (!json && console != null)
            {
                console.WriteSuccess($"✅ Wrote {output.FullName}");
            }
            else if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    input = input.FullName,
                    output = output.FullName
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
}
