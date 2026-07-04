using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

internal sealed record CompositionMutationTestResult(
    int TotalMutants,
    IReadOnlyList<string> SurvivingMutantIds,
    IReadOnlyList<string> KilledMutantIds,
    double EscapeRate);
