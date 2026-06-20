namespace Nexo.Spike.S3.Models;

/// <summary>
/// One admitted skill in the file-backed registry.
/// </summary>
public sealed record RegistryEntry(
    string EntryId,
    IntentDescriptor Intent,
    string CandidateId,
    string ContentHash,
    string ImplementationSource,
    string IntentSpecPath,
    SignedCertificationRecord Certification,
    DateTimeOffset AdmittedAt);
