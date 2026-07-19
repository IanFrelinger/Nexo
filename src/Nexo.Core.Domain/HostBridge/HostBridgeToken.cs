using System;

namespace Nexo.Core.Domain.HostBridge;

/// <summary>Shared secret used to authorize host-bridge commands.</summary>
public readonly struct HostBridgeToken : IEquatable<HostBridgeToken>
{
    /// <summary>Creates a token from a non-empty string.</summary>
    public HostBridgeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Token value is required.", nameof(value));
        Value = value;
    }

    /// <summary>Raw token string.</summary>
    public string Value { get; }

    /// <summary>Creates a new random token.</summary>
    public static HostBridgeToken CreateRandom()
        => new(Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public bool Equals(HostBridgeToken other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is HostBridgeToken other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(HostBridgeToken left, HostBridgeToken right)
        => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(HostBridgeToken left, HostBridgeToken right)
        => !left.Equals(right);
}
