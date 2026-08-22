using Microsoft.Extensions.Logging;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;

namespace Ashlar.Bricks.Owasp.Security;

public record ScanMetadata(
    int RulesApplied,
    int LinesScanned,
    string Language
);
