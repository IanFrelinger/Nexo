using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands.Runtime;

internal sealed record RuntimeRemediationAttempt(
    string Policy,
    string Reason,
    bool Ok,
    string? FailureStage,
    string? RunId,
    string Summary);
