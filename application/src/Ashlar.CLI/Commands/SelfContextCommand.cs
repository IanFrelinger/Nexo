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
            await ExecuteAsync(lookbackStr);
        });
    }

    private static async Task ExecuteAsync(string? lookbackStr)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(RepoPathResolver.ResolveStateDirectory(repoRoot), "ashlar-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath)
            .BuildServiceProvider();

        var assembler = services.GetRequiredService<ISelfContextAssembler>();

        var lookback = ParseLookback(lookbackStr ?? "24h");
        var selfContext = await assembler.AssembleAsync(lookback).ConfigureAwait(false);

        Console.WriteLine(selfContext.Summary);
    }

    private static TimeSpan ParseLookback(string s)
    {
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("h"))
        {
            var n = int.Parse(s.TrimEnd('h'));
            return TimeSpan.FromHours(n);
        }
        if (s.EndsWith("d"))
        {
            var n = int.Parse(s.TrimEnd('d'));
            return TimeSpan.FromDays(n);
        }
        if (s.EndsWith("m"))
        {
            var n = int.Parse(s.TrimEnd('m'));
            return TimeSpan.FromMinutes(n);
        }
        return TimeSpan.FromHours(24);
    }
}
