using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

internal sealed record CompositionRunResult(
    bool Success,
    IReadOnlyDictionary<string, object> Output,
    IReadOnlyList<string> Errors);
