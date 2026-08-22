using Microsoft.Extensions.Logging;

namespace Ashlar.Tests.Infrastructure.Barriers.Identity;

internal sealed record TestLogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties);
