using Microsoft.Extensions.Logging;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;

namespace Ashlar.Bricks.Owasp.Security;

public record SecurityFinding(
    string Id,
    string Type,
    string Severity,
    string? CweId,
    string Description
);
