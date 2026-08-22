using System.Text.Json;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification.Composition;

internal sealed record WitnessRunResult(bool Passed, IReadOnlyList<string> Failures);
