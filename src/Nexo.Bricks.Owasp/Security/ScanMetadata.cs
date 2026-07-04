using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Bricks.Owasp.Security;

public record ScanMetadata(
    int RulesApplied,
    int LinesScanned,
    string Language
);
