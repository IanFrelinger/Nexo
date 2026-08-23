using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeRemediationAttempt(
    string Policy,
    string Reason,
    bool Ok,
    string? FailureStage,
    string? RunId,
    string Summary);
