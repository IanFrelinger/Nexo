using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Analysis;
using Ashlar.Infrastructure.Observation;
using Ashlar.Infrastructure.SelfContext;

namespace Ashlar.CLI.Commands;

/// <summary>
/// Block 6: Query self-context — what did Ashlar change recently and did it improve things?
/// </summary>
public sealed class SelfContextCommand : Command
{
    /// <summary>Creates a new SelfContextCommand instance.</summary>
    public SelfContextCommand() : base("self-context", "Assemble and display self-context: recent adaptations, executions, patterns.")
    {
        var lookbackOpt = new Option<string?>("--lookback", "Lookback duration (e.g. 24h, 1d, 7d). Default: 24h");

        AddOption(lookbackOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var lookbackStr = ctx.ParseResult.GetValueForOption(lookbackOpt);
            // #455: Environment.ExitCode is overwritten back to 0 after the handler returns.
            ctx.ExitCode = await ExecuteAsync(lookbackStr);
        });
    }

    private static async Task<int> ExecuteAsync(string? lookbackStr)
    {
        var lookbackText = string.IsNullOrWhiteSpace(lookbackStr) ? "24h" : lookbackStr;
        if (!TryParseLookback(lookbackText, out var lookback))
        {
            Console.Error.WriteLine("Invalid --lookback. Use a duration such as 24h, 1d, or 30m.");
            return 1;
        }

        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(RepoPathResolver.ResolveStateDirectory(repoRoot), "ashlar-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath)
            .BuildServiceProvider();

        var assembler = services.GetRequiredService<ISelfContextAssembler>();
        var selfContext = await assembler.AssembleAsync(lookback).ConfigureAwait(false);

        Console.WriteLine(selfContext.Summary);
        return 0;
    }

    private static bool TryParseLookback(string s, out TimeSpan lookback)
    {
        lookback = default;
        s = s.Trim().ToLowerInvariant();
        if (s.Length < 2)
            return false;

        var unit = s[^1];
        if (!int.TryParse(s[..^1], out var n) || n < 0)
            return false;

        lookback = unit switch
        {
            'h' => TimeSpan.FromHours(n),
            'd' => TimeSpan.FromDays(n),
            'm' => TimeSpan.FromMinutes(n),
            _ => default
        };
        return unit is 'h' or 'd' or 'm';
    }
}
