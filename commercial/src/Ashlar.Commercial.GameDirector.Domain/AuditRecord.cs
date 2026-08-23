using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ashlar.Core.Application.Trust.Models;
using Ashlar.Core.Application.Trust.Ports;

namespace GameDirector.Domain;

public sealed record AuditRecord(
    string AuditId,
    DateTimeOffset Timestamp,
    string Tool,
    string InputHash,
    string ModelId,
    string TrustPolicy,
    string OutputSummary,
    IReadOnlyList<string> SanitizationEvents);
