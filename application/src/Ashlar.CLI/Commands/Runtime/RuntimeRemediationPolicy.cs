using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

internal sealed record RuntimeRemediationPolicy(string Policy, string Reason);
