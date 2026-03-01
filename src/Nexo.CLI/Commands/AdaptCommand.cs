using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Adaptation;

namespace Nexo.CLI.Commands;

/// <summary>
/// Dogfood command: decompose a brick to manifest, optionally apply fixes, recompile.
/// Block 3 adaptation engine.
/// </summary>
public sealed class AdaptCommand : Command
{
    public AdaptCommand() : base("adapt", "Decompose brick to manifest, optionally apply fixes and recompile (Block 3 dogfood).")
    {
        var brickOpt = new Option<string>("--brick", () => "observation.context", "Brick ID to adapt");
        var fixOpt = new Option<string?>("--fix", "Apply fix for failure type (e.g. EmptyCatch, MissingOutput)");
        var dryRunOpt = new Option<bool>("--dry-run", () => false, "Only decompose; do not recompile");

        AddOption(brickOpt);
        AddOption(fixOpt);
        AddOption(dryRunOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var brickId = ctx.ParseResult.GetValueForOption(brickOpt) ?? "observation.context";
            var fixType = ctx.ParseResult.GetValueForOption(fixOpt);
            var dryRun = ctx.ParseResult.GetValueForOption(dryRunOpt);
            await ExecuteAsync(brickId, fixType, dryRun);
        });
    }

    private static async Task ExecuteAsync(string brickId, string? fixType, bool dryRun)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var storePath = Path.Combine(repoRoot, "nexo-patterns.db");

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddAdaptationInfrastructure(storePath)
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<AdaptCommand>();

        var decomposer = services.GetRequiredService<IBrickDecomposer>();
        var fixGenerator = services.GetRequiredService<IFixGenerator>();
        var recompiler = services.GetRequiredService<IBrickRecompiler>();

        Nexo.Core.Domain.Bricks.Brick? brick = brickId == "observation.context"
            ? new ObservationContextBrick(services.GetRequiredService<IContextAssembler>())
            : null;

        if (brick == null)
        {
            logger.LogError("Brick {Id} not supported for adapt. Use observation.context.", brickId);
            Environment.ExitCode = 1;
            return;
        }

        var manifest = await decomposer.DecomposeAsync(brick).ConfigureAwait(false);
        Console.WriteLine($"Decomposed {brickId} -> manifest {manifest.Id} v{manifest.Version}");
        Console.WriteLine($"  Interface: {manifest.Interface.Inputs.Count} inputs, {manifest.Interface.Outputs.Count} outputs");
        Console.WriteLine($"  ImplementationTypeName: {manifest.ImplementationTypeName ?? "(none)"}");

        BrickManifest toRecompile = manifest;
        if (!string.IsNullOrEmpty(fixType))
        {
            var context = new FailureContext
            {
                FailureType = fixType,
                TargetId = brickId,
                Message = $"Applied fix for {fixType}",
            };
            var fixes = await fixGenerator.GenerateFixesAsync(context, manifest).ConfigureAwait(false);
            if (fixes.Count > 0)
            {
                toRecompile = fixes[0];
                Console.WriteLine($"  Applied fix: {fixType} -> v{toRecompile.Version}");
            }
        }

        if (dryRun)
        {
            Console.WriteLine("(dry-run: skip recompile)");
            Environment.ExitCode = 0;
            return;
        }

        var recompiled = await recompiler.RecompileAsync(toRecompile).ConfigureAwait(false);
        if (recompiled != null)
        {
            Console.WriteLine($"  Recompiled OK: {recompiled.Id}");
        }
        else
        {
            logger.LogWarning("Recompile returned null (known type may need DI).");
        }

        Environment.ExitCode = 0;
    }
}
