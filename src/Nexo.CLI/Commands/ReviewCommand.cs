using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Review and summary commands. Replaces scripts/review-summary-md.sh.
/// </summary>
public class ReviewCommand : Command
{
    public ReviewCommand() : base("review", "Review and summary (replaces review-summary-md.sh)")
    {
        var inputOpt = new Option<FileInfo>("--input", () => new FileInfo("review_summary.json"), "Input JSON file");
        var outputOpt = new Option<FileInfo>("--output", () => new FileInfo("REVIEW_SUMMARY.md"), "Output Markdown file");

        var summaryCmd = new Command("summary", "Write review summary JSON to Markdown");
        summaryCmd.AddOption(inputOpt);
        summaryCmd.AddOption(outputOpt);
        summaryCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var input = ctx.ParseResult.GetValueForOption(inputOpt) ?? new FileInfo("review_summary.json");
            var output = ctx.ParseResult.GetValueForOption(outputOpt) ?? new FileInfo("REVIEW_SUMMARY.md");
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null && ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = verboseOpt != null && ctx.ParseResult.GetValueForOption(verboseOpt);
            var exitCode = await SummaryAsync(input, output, json, verbose);
            ctx.ExitCode = exitCode;
        });

        AddCommand(summaryCmd);
    }

    private static async Task<int> SummaryAsync(FileInfo input, FileInfo output, bool json, bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!input.Exists || input.Length == 0)
        {
            await File.WriteAllTextAsync(output.FullName, "# Review Summary\n\n_No review_summary.json found._\n");
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { success = true, message = "Empty summary written" }));
            else console?.WriteSuccess($"Wrote {output.FullName}");
            return 0;
        }

        try
        {
            var content = await File.ReadAllTextAsync(input.FullName);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Review Summary");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine(content);
            sb.AppendLine("```");
            sb.AppendLine();

            try
            {
                var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("seeds", out var seeds) && seeds.ValueKind == JsonValueKind.Array)
                {
                    var allPlayable = true;
                    var anyFallbacks = false;
                    foreach (var seed in seeds.EnumerateArray())
                    {
                        if (seed.TryGetProperty("status", out var status) && status.GetString() != "playable") allPlayable = false;
                        if (seed.TryGetProperty("fallbacksUsed", out var fb) && fb.ValueKind == JsonValueKind.True) anyFallbacks = true;
                    }
                    sb.AppendLine($"- **Playable across seeds:** {allPlayable}");
                    sb.AppendLine($"- **Fallbacks used:** {anyFallbacks}");
                    sb.AppendLine();
                }
            }
            catch
            {
                // Best-effort verdict
            }

            if (output.Directory != null && !output.Directory.Exists) output.Directory.Create();
            await File.WriteAllTextAsync(output.FullName, sb.ToString());

            if (json)
                Console.WriteLine(JsonSerializer.Serialize(new { success = true, outputFile = output.FullName }));
            else
                console?.WriteSuccess($"Wrote {output.FullName}");
            return 0;
        }
        catch (Exception ex)
        {
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            else console?.WriteError(ex.Message);
            return 1;
        }
    }
}
