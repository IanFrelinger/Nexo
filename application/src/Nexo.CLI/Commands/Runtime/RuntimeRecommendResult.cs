using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeRecommendResult(
    bool Ok,
    string Summary,
    string? Policy = null,
    string? Reason = null);
