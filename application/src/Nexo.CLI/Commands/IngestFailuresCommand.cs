using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.SelfContext;
using Nexo.Infrastructure.SelfImprovement;
using Nexo.Infrastructure.Validation.Parsers;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command that parses TRX test result files and ingests failures into
/// the ITestFailureStore, closing the loop for self-improvement.
/// Usage: nexo ingest-failures --trx-path ./TestResults
/// </summary>
public sealed class IngestFailuresCommand : Command
{
    /// <summary>Creates a new IngestFailuresCommand instance.</summary>
    public IngestFailuresCommand() : base("ingest-failures", "Parse TRX test result files and ingest failures into the self-improvement store.")
    {
        var trxPathOpt = new Option<string>("--trx-path", "Path to a TRX file or directory containing TRX files") { IsRequired = true };
        var storePathOpt = new Option<string?>("--store-path", "Directory for nexo dbs (default: repo root)");

        AddOption(trxPathOpt);
        AddOption(storePathOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var trxPath = ctx.ParseResult.GetValueForOption(trxPathOpt)!;
            var storePathOverride = ctx.ParseResult.GetValueForOption(storePathOpt);
            await ExecuteAsync(trxPath, storePathOverride);
        });
    }

    private static async Task ExecuteAsync(string trxPath, string? storePathOverride)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = !string.IsNullOrWhiteSpace(storePathOverride)
            ? Path.Combine(Path.GetFullPath(storePathOverride), "nexo-patterns.db")
            : Path.Combine(repoRoot, "nexo-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .AddSelfContextInfrastructure(storePath)
            .BuildServiceProvider();

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var parser = new TrxTestResultParser(loggerFactory.CreateLogger<TrxTestResultParser>());
        var store = services.GetRequiredService<Nexo.Core.Application.SelfContext.Ports.ITestFailureStore>();
        var bridge = new TestFailureIngestionBridge(store);

        var trxFiles = ResolveTrxFiles(trxPath);
        if (trxFiles.Count == 0)
        {
            Console.WriteLine($"No TRX files found at: {trxPath}");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Found {trxFiles.Count} TRX file(s).");
        var totalIngested = 0;

        foreach (var file in trxFiles)
        {
            var results = await parser.ParseAsync(file).ConfigureAwait(false);
            var ingested = await bridge.IngestAsync(results).ConfigureAwait(false);
            totalIngested += ingested;
            Console.WriteLine($"  {file.Name}: {results.Count} result(s), {ingested} failure(s) ingested");
        }

        Console.WriteLine($"Total: {totalIngested} failure(s) ingested into store.");
        Environment.ExitCode = 0;
    }

    private static List<FileInfo> ResolveTrxFiles(string path)
    {
        if (File.Exists(path) && path.EndsWith(".trx", StringComparison.OrdinalIgnoreCase))
            return new List<FileInfo> { new FileInfo(path) };

        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.trx", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();
        }

        return new List<FileInfo>();
    }
}
