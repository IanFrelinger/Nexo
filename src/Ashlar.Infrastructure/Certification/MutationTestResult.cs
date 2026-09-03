using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Outcome of a mutation run. <c>Survivors</c> carries each surviving mutant with the edit
/// that produced it, so an operator can tell a weak witness from an equivalent mutant without
/// decoding the candidate by hand; <c>SurvivingMutantIds</c> stays authoritative for the count.
/// </summary>
/// <remarks>
/// A dead mutant is dead for exactly one of three reasons, and the record keeps them apart:
/// the WITNESS caught it (<c>KilledMutantIds</c>, the only kind of death that shows the witness
/// has teeth — plus mutants dead on arrival at an earlier gate: non-compiling, identical,
/// analyzer-refused), the WALL CLOCK stopped it (<c>TimedOutMutantIds</c>), or executing it
/// KILLED the process it ran in (<c>CrashedMutantIds</c>). The last two are behavioural facts
/// that make the mutant uncertifiable, but they say nothing about the witness, and a
/// certificate that filed them under kills would claim the witness caught what the clock caught.
/// <c>TotalMutants</c> is the sum of all four lists.
/// </remarks>
internal sealed record MutationTestResult(
    int TotalMutants,
    IReadOnlyList<string> SurvivingMutantIds,
    IReadOnlyList<string> KilledMutantIds,
    double EscapeRate,
    IReadOnlyList<MutationSurvivor>? Survivors = null,
    IReadOnlyList<string>? TimedOutMutantIds = null,
    IReadOnlyList<string>? CrashedMutantIds = null)
{
    /// <summary>The surviving mutants with their sites; never null.</summary>
    public IReadOnlyList<MutationSurvivor> Survivors { get; init; } = Survivors ?? [];

    /// <summary>Mutants the wall clock stopped; never null.</summary>
    public IReadOnlyList<string> TimedOutMutantIds { get; init; } = TimedOutMutantIds ?? [];

    /// <summary>Mutants whose execution killed the process running them; never null.</summary>
    public IReadOnlyList<string> CrashedMutantIds { get; init; } = CrashedMutantIds ?? [];
}
