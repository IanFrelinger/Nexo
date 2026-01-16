using Nexo.CLI.Demo.SelfExtend.Pipeline.Steps;
using Nexo.Core.Application.Interfaces;

namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

public interface ISelfExtendPlanner
{
    IReadOnlyList<ICommand<SelfExtendContext, SelfExtendContext>> Next(SelfExtendContext ctx);
}

/// <summary>
/// Deterministic planner that demonstrates runtime composition:
/// it inserts/removes/reorders steps based on observed test outcomes.
/// </summary>
public sealed class DeterministicSelfExtendPlanner : ISelfExtendPlanner
{
    private readonly SelfExtendToolRuntime _rt;
    private int _stage;

    public DeterministicSelfExtendPlanner(SelfExtendToolRuntime rt) => _rt = rt;

    public IReadOnlyList<ICommand<SelfExtendContext, SelfExtendContext>> Next(SelfExtendContext ctx)
    {
        // Stage 0: write test + intentionally wrong implementation to force a red phase.
        if (_stage == 0)
        {
            _stage = 1;
            return new ICommand<SelfExtendContext, SelfExtendContext>[]
            {
                new WriteGeneratedCommandCommand(_rt, messageOverride: "TODO: fix me"),
                new DotnetBuildCommand(_rt),
                new VerifyGeneratedCommandCommand(_rt)
            };
        }

        // Stage 1: if tests failed, fix implementation and rerun tests (green phase).
        if (_stage == 1)
        {
            _stage = 2;
            if (ctx.LastTestOk == true)
            {
                return Array.Empty<ICommand<SelfExtendContext, SelfExtendContext>>();
            }

            return new ICommand<SelfExtendContext, SelfExtendContext>[]
            {
                new WriteGeneratedCommandCommand(_rt, messageOverride: ctx.Spec.Message),
                new VerifyGeneratedCommandCommand(_rt)
            };
        }

        return Array.Empty<ICommand<SelfExtendContext, SelfExtendContext>>();
    }
}

