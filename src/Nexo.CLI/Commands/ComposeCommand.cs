using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Composition.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Composition;

namespace Nexo.CLI.Commands;

/// <summary>
/// Block 7: Compose an agent from capability components at runtime.
/// </summary>
public sealed class ComposeCommand : Command
{
    public ComposeCommand() : base("compose", "Compose an agent from capability components (Block 7).")
    {
        var problemOpt = new Option<string>("--problem", "Problem description (e.g. 'test Nexo CLI', 'test failure analyzer')");
        problemOpt.IsRequired = true;
        var capabilitiesOpt = new Option<string[]>("--capabilities", "Available capabilities (default: perception, validation, reporting)");
        capabilitiesOpt.AllowMultipleArgumentsPerToken = true;

        AddOption(problemOpt);
        AddOption(capabilitiesOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var problem = ctx.ParseResult.GetValueForOption(problemOpt)!;
            var capabilities = ctx.ParseResult.GetValueForOption(capabilitiesOpt) ?? Array.Empty<string>();
            await ExecuteAsync(problem, capabilities);
        });
    }

    private static async Task ExecuteAsync(string problem, string[] capabilities)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddCompositionInfrastructure()
            .BuildServiceProvider();

        var engine = services.GetRequiredService<ICompositionEngine>();
        var registry = services.GetRequiredService<ICapabilityComponentRegistry>();

        var available = capabilities.Length > 0
            ? capabilities.ToList()
            : new List<string> { "perception", "validation", "reporting", "understanding", "code-analysis" };

        var composed = await engine.ComposeAsync(problem, available).ConfigureAwait(false);

        if (composed == null)
        {
            Console.WriteLine("No composition found for the given problem and capabilities.");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine("Composed Agent");
        Console.WriteLine($"  Problem: {composed.ProblemDescription}");
        Console.WriteLine($"  Pipeline: {string.Join(" → ", composed.ComponentIds)}");
        Console.WriteLine();
        Console.WriteLine("Components:");
        foreach (var id in composed.ComponentIds)
        {
            var desc = registry.GetById(id);
            Console.WriteLine($"  - {id}: {desc?.Capability ?? "?"} ({desc?.ImplementationType ?? "?"})");
        }
        Environment.ExitCode = 0;
    }
}
