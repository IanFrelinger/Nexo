using Microsoft.Extensions.Logging;

namespace Nexo.Tests.Infrastructure.Barriers.Sinks;

internal sealed record TestLogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties);
