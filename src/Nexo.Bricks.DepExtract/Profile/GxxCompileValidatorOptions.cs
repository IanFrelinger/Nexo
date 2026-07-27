namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Options for <see cref="GxxCompileValidator"/>. Paths and image names live
/// in the C++ profile plugin — never in core ports.
/// </summary>
public sealed class GxxCompileValidatorOptions
{
    /// <summary>Poco/evtx root containing <c>common/processing.hpp</c>.</summary>
    public string? PocoContext { get; init; }

    /// <summary>Extracted proprietary parser tree (optional).</summary>
    public string? ParserDir { get; init; }

    /// <summary>Generated Qt shim header directory (optional).</summary>
    public string? ShimDir { get; init; }

    /// <summary>
    /// When true, missing POCO/Docker is a hard validation failure.
    /// When false, those conditions soft-pass (skip) so COMPILE_GATE=0 paths work.
    /// </summary>
    public bool RequireSuccess { get; init; }

    /// <summary>Opaque sandbox image identifier (default: dep-extract:latest).</summary>
    public string Image { get; init; } = "dep-extract:latest";

    /// <summary>Sandbox entrypoint override (default: g++).</summary>
    public string Entrypoint { get; init; } = "g++";
}
