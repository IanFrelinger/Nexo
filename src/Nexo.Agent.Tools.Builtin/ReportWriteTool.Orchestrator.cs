using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Agent.Abstractions;
        using var activity = new ActivitySource("Nexo.Tool").StartActivity("ReportWrite");

namespace Nexo.Agent.Tools.Builtin;
{
}