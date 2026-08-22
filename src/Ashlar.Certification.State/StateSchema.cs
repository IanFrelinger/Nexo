using System.Text.Json;

namespace Ashlar.Certification.State;

/// <summary>
/// Content-bound description of the shape state must satisfy.
/// <see cref="SchemaHash"/> is produced from <see cref="CanonicalForm"/> via <c>BrickContentHasher.ComputeSha256</c>.
/// </summary>
public sealed class StateSchema
{
    private static readonly System.Text.RegularExpressions.Regex Base64Sha256Pattern = new(
        @"^[A-Za-z0-9+/]{43}=$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant
#if NET5_0_OR_GREATER
        | System.Text.RegularExpressions.RegexOptions.Compiled
#endif
        );

    private readonly SchemaRules _rules;

    public StateSchema(string canonicalForm)
    {
        if (string.IsNullOrWhiteSpace(canonicalForm))
            throw new ArgumentException("Canonical form is required.", nameof(canonicalForm));

        CanonicalForm = canonicalForm;
        SchemaHash = Contracts.BrickContentHasher.ComputeSha256(canonicalForm);
        _rules = ParseRules(canonicalForm);
    }

    public string CanonicalForm { get; }

    public string SchemaHash { get; }

    /// <summary>
    /// Tier-1 structural schema check on a state hash (format + length).
    /// Semantic correctness of the underlying state value requires Tier-2 replay.
    /// </summary>
    public bool IsValidStateHash(string stateHash)
    {
        if (string.IsNullOrWhiteSpace(stateHash))
            return false;

        if (stateHash.Length != _rules.HashLength)
            return false;

        if (!Base64Sha256Pattern.IsMatch(stateHash))
            return false;

        return TryDecodeBase64(stateHash, out _);
    }

    /// <summary>
    /// Computes a schema-bound state hash for witnesses and replay fixtures.
    /// Binding embeds <see cref="SchemaHash"/> so state hashes cannot be reused across schemas.
    /// </summary>
    public string ComputeBoundStateHash(string statePayload)
    {
        if (string.IsNullOrEmpty(statePayload))
            throw new ArgumentException("State payload is required.", nameof(statePayload));

        var bound = $"STATE|{_rules.BindingVersion}|{SchemaHash}|{statePayload}";
        return Contracts.BrickContentHasher.ComputeSha256(bound);
    }

    private static SchemaRules ParseRules(string canonicalForm)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalForm);
            var root = document.RootElement;

            var version = root.GetProperty("version").GetInt32();
            if (version != 1)
                throw new ArgumentException($"Unsupported schema version: {version}.", nameof(canonicalForm));

            var binding = root.GetProperty("stateBinding");
            var bindingVersion = binding.GetProperty("version").GetString()
                ?? throw new ArgumentException("stateBinding.version is required.", nameof(canonicalForm));
            var hashLength = binding.GetProperty("hashLength").GetInt32();

            return new SchemaRules(bindingVersion, hashLength);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Canonical form must be valid schema JSON.", nameof(canonicalForm), ex);
        }
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    private sealed record SchemaRules(string BindingVersion, int HashLength);
}
