using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeRecommendResult(
    bool Ok,
    string Summary,
    string? Policy = null,
    string? Reason = null);
