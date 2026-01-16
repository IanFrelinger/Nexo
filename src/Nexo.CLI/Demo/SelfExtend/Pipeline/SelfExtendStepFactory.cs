using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;
using Nexo.Core.Application.Interfaces;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

public interface ISelfExtendStepFactory
{
    ICommand<SelfExtendContext, SelfExtendContext> Create(string stepId, StepSpec spec);
}

/// <summary>
/// Creates steps with selectable implementations (LLM first / deterministic first) based on runtime config.
/// </summary>
public sealed class SelfExtendStepFactory : ISelfExtendStepFactory
{
    private readonly SelfExtendToolRuntime _rt;
    private readonly IProviderFactory _providerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SelfExtendRuntimeConfig _cfg;

    public SelfExtendStepFactory(
        SelfExtendToolRuntime rt,
        IProviderFactory providerFactory,
        ILoggerFactory loggerFactory,
        SelfExtendRuntimeConfig cfg)
    {
        _rt = rt;
        _providerFactory = providerFactory;
        _loggerFactory = loggerFactory;
        _cfg = cfg;
    }

    public ICommand<SelfExtendContext, SelfExtendContext> Create(string stepId, StepSpec spec)
    {
        var impls = new List<(string kind, ICommand<SelfExtendContext, SelfExtendContext> cmd)>();
        foreach (var impl in spec.Implementations)
        {
            impls.Add((impl.Type.ToLowerInvariant(), CreateImpl(spec, impl)));
        }

        // Reorder implementations to match Prefer ("llm" or "deterministic")
        var prefer = (spec.Prefer ?? _cfg.Prefer ?? "llm").Trim().ToLowerInvariant();
        impls = impls
            .OrderByDescending(x => x.kind == prefer)
            .ToList();

        return new SelectableStepCommand(
            stepId,
            impls,
            _loggerFactory.CreateLogger<SelectableStepCommand>());
    }

    private ICommand<SelfExtendContext, SelfExtendContext> CreateImpl(StepSpec spec, ImplSpec impl)
    {
        var type = impl.Type.Trim().ToLowerInvariant();
        return (spec.Kind.Trim().ToLowerInvariant(), type) switch
        {
            ("write_command", "deterministic") => new WriteGeneratedCommandCommand(_rt, messageOverride: GetOptionalMessageArg(spec)),
            ("write_command", "llm") => new LlmWriteGeneratedCommandCommand(_rt, _providerFactory, _cfg.Provider, messageOverride: GetOptionalMessageArg(spec)),

            ("dotnet_build", "deterministic") => new DotnetBuildCommand(_rt),

            ("nexo_test", "deterministic") => new RunNexoTestCommand(_rt, filter: GetFilterArg(spec)),

            ("roslyn_analyze", "deterministic") => new RoslynAnalyzeGeneratedCommandCommand(_rt),

            ("verify_command", "deterministic") => new VerifyGeneratedCommandCommand(_rt),

            _ => throw new InvalidOperationException($"Unknown step kind '{spec.Kind}' / impl '{type}'")
        };
    }

    private static string GetFilterArg(StepSpec spec)
    {
        if (spec.Args != null && spec.Args.TryGetValue("filter", out var el) && el.ValueKind == JsonValueKind.String)
        {
            return el.GetString() ?? "DemoGenerated";
        }
        return "DemoGenerated";
    }

    private static string? GetOptionalMessageArg(StepSpec spec)
    {
        if (spec.Args != null && spec.Args.TryGetValue("message", out var el) && el.ValueKind == JsonValueKind.String)
        {
            return el.GetString();
        }
        return null;
    }
}

