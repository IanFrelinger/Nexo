using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification.Composition;

internal sealed record CompositionMutationTestResult(
    int TotalMutants,
    IReadOnlyList<string> SurvivingMutantIds,
    IReadOnlyList<string> KilledMutantIds,
    double EscapeRate);
