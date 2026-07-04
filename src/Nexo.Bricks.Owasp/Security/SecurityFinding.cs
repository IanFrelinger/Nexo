using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Bricks.Owasp.Security;

public record SecurityFinding(
    string Id,
    string Type,
    string Severity,
    string? CweId,
    string Description
);
