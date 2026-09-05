using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.SelfContext;

namespace Ashlar.CLI.Commands;

/// <summary>
/// Phase F: Generate changelog from promoted adaptation records.
/// </summary>
public sealed class ChangelogCommand : Command
{
    /// <summary>Creates a new ChangelogCommand instance.</summary>
    public ChangelogCommand() : base("changelog", "Generate changelog from promoted changes (Phase F).")
    {
        var sinceOpt = new Option<string?>("--since", "Start date (e.g. 7d, 30d, or yyyy-MM-dd). Default: 7d");
        var untilOpt = new Option<string?>("--until", "End date (e.g. yyyy-MM-dd). Default: now");
        var outputOpt = new Option<FileInfo?>("--output", "Write to file. Default: stdout");

        AddOption(sinceOpt);
        AddOption(untilOpt);
        AddOption(outputOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var sinceStr = ctx.ParseResult.GetValueForOption(sinceOpt);
            var untilStr = ctx.ParseResult.GetValueForOption(untilOpt);
            var output = ctx.ParseResult.GetValueForOption(outputOpt);
            // #455: Environment.ExitCode is overwritten back to 0 after the handler returns.
            ctx.ExitCode = await ExecuteAsync(sinceStr, untilStr, output);
        });
    }

    private static async Task<int> ExecuteAsync(string? sinceStr, string? untilStr, FileInfo? output)
    {
        var sinceText = string.IsNullOrWhiteSpace(sinceStr) ? "7d" : sinceStr;
        if (!TryParseSince(sinceText, out var since))
        {
            Console.Error.WriteLine("Invalid --since. Use a duration such as 7d or 30h, or a date (yyyy-MM-dd).");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(untilStr) && !DateTimeOffset.TryParse(untilStr, out _))
        {
            Console.Error.WriteLine("Invalid --until. Use a date (yyyy-MM-dd).");
            return 1;
        }

        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(RepoPathResolver.ResolveStateDirectory(repoRoot), "ashlar-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .AddChangelogGenerator()
            .BuildServiceProvider();

        var generator = services.GetRequiredService<IChangelogGenerator>();
        var until = ParseUntil(untilStr);

        var changelog = await generator.GenerateAsync(since, until).ConfigureAwait(false);

        if (output != null)
        {
            await File.WriteAllTextAsync(output.FullName, changelog).ConfigureAwait(false);
            Console.WriteLine($"Changelog written to {output.FullName}");
        }
        else
        {
            Console.WriteLine(changelog);
        }

        return 0;
    }

    private static bool TryParseSince(string s, out DateTimeOffset since)
    {
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("d") && int.TryParse(s[..^1], out var days) && days >= 0)
        {
            since = DateTimeOffset.UtcNow.AddDays(-days);
            return true;
        }

        if (s.EndsWith("h") && int.TryParse(s[..^1], out var hours) && hours >= 0)
        {
            since = DateTimeOffset.UtcNow.AddHours(-hours);
            return true;
        }

        return DateTimeOffset.TryParse(s, out since);
    }

    private static DateTimeOffset? ParseUntil(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTimeOffset.TryParse(s, out var parsed) ? parsed : null;
    }
}
