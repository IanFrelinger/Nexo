namespace Nexo.Spike.S0.Models;

/// <summary>Which gate rejected a run (instrumentation).</summary>
public enum GuardKind
{
    None,
    OpenQuestions,
    Tautology,
    RedReason,
    Mutation,
    Budget
}
