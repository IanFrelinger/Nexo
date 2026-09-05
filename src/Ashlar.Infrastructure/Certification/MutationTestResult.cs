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
internal sealed record MutationTestResult(
    int TotalMutants,
    IReadOnlyList<string> SurvivingMutantIds,
    IReadOnlyList<string> KilledMutantIds,
    double EscapeRate,
    IReadOnlyList<MutationSurvivor>? Survivors = null)
{
    /// <summary>The surviving mutants with their sites; never null.</summary>
    public IReadOnlyList<MutationSurvivor> Survivors { get; init; } = Survivors ?? [];
}
