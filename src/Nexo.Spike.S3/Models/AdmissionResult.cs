namespace Nexo.Spike.S3.Models;

public enum AdmissionStatus
{
    Admitted,
    Rejected
}

public sealed record AdmissionResult(
    AdmissionStatus Status,
    RegistryEntry? Entry,
    string? RejectionReason);
