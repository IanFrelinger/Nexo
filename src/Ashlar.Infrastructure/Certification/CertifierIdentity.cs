using Ashlar.Certification.Contracts;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Names the judge. Recorded as a signed <c>certifier-identity</c> input on every disk certify.</summary>
public static class CertifierIdentity
{
    /// <summary>Stable gate type name written on the record.</summary>
    public const string GateName = "Ashlar.Infrastructure.Certification.CertificationGate";

    /// <summary>Rule-set identifier covering the five legs plus the round-10 fences.</summary>
    public const string RuleSet = "atom-five-leg+compile-authority-v1";

    /// <summary>Canonical identity blob hashed into the certificate.</summary>
    public static string CanonicalBlob { get; } =
        $"gate={GateName};rules={RuleSet};compiler-ceiling={BrickCompileOptions.LanguageVersionName};compile-options={BrickCompileOptions.CanonicalBlob}";

    /// <summary>Signed input naming this judge.</summary>
    public static CertificationInput ToInput() => new()
    {
        Kind = CertificationInputKinds.CertifierIdentity,
        Id = GateName,
        Hash = BrickContentHasher.ComputeSha256(CanonicalBlob)
    };
}
