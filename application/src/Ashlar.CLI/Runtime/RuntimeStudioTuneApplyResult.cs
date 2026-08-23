using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ashlar.CLI.Runtime;

/// <summary>Creates a new RuntimeStudioTuneApplyResult instance.</summary>
public sealed record RuntimeStudioTuneApplyResult(bool Ok, string Summary, IReadOnlyList<string>? UpdatedAgentIds = null);
