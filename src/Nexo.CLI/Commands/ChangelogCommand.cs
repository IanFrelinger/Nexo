using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.SelfContext;

namespace Nexo.CLI.Commands;

/// <summary>
/// Phase F: Generate changelog from promoted adaptation records.
/// </summary>
public sealed class ChangelogCommand : Command
{
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
            await ExecuteAsync(sinceStr, untilStr, output);
        });
    }

    private static async Task ExecuteAsync(string? sinceStr, string? untilStr, FileInfo? output)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(repoRoot, "nexo-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddAdaptationInfrastructure(storePath)
            .AddChangelogGenerator()
            .BuildServiceProvider();

        var generator = services.GetRequiredService<IChangelogGenerator>();

        var since = ParseSince(sinceStr ?? "7d");
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

        Environment.ExitCode = 0;
    }

    private static DateTimeOffset ParseSince(string s)
    {
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("d"))
        {
            var n = int.Parse(s.TrimEnd('d'));
            return DateTimeOffset.UtcNow.AddDays(-n);
        }
        if (s.EndsWith("h"))
        {
            var n = int.Parse(s.TrimEnd('h'));
            return DateTimeOffset.UtcNow.AddHours(-n);
        }
        if (DateTimeOffset.TryParse(s, out var parsed))
            return parsed;
        return DateTimeOffset.UtcNow.AddDays(-7);
    }

    private static DateTimeOffset? ParseUntil(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTimeOffset.TryParse(s, out var parsed) ? parsed : null;
    }
}
