using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

internal sealed record WitnessRunResult(bool Passed, IReadOnlyList<string> Failures);
