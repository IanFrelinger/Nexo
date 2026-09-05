namespace Ashlar.Contracts.Distributed;

internal static class DistributedContractGuard
{
    internal static IReadOnlyList<string>? Capabilities(IReadOnlyList<string>? capabilities)
    {
        if (capabilities is null)
        {
            return null;
        }

        var trimmed = new string[capabilities.Count];
        for (var i = 0; i < capabilities.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(capabilities[i]))
            {
                throw new ArgumentException("Capability names must be non-blank.", nameof(capabilities));
            }

            trimmed[i] = capabilities[i].Trim();
        }

        return trimmed;
    }

    internal static void Timestamp(DateTimeOffset value, string paramName)
    {
        if (value == default)
        {
            throw new ArgumentOutOfRangeException(paramName, "Timestamp must be set.");
        }
    }

    internal static void Duration(TimeSpan? value, string paramName)
    {
        if (value is { } duration && duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, "Duration must be positive when set.");
        }
    }

    internal static void Defined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Enum value is not defined.");
        }
    }

    internal static string Digest(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return NormalizeDigest(value, paramName);
    }

    internal static string OptionalDigest(string value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Output hash cannot be whitespace.", paramName);
        }

        return NormalizeDigest(value, paramName);
    }

    private static string NormalizeDigest(string value, string paramName)
    {
        var trimmed = value.Trim();
        foreach (var c in trimmed)
        {
            if (char.IsWhiteSpace(c) ||
                !(char.IsAsciiLetterOrDigit(c) || c is ':' or '+' or '/' or '=' or '_' or '.' or '-'))
            {
                throw new ArgumentException(
                    "Digest must be hex, multibase, or algorithm:value without whitespace.",
                    paramName);
            }
        }

        return trimmed;
    }
}
