namespace Ashlar.Core.Application.Orchestration.Profiles;

/// <summary>Feature flags for assembly analysis orchestration profiles.</summary>
/// <param name="EnableDecompile">When true, run decompilation analysis steps.</param>
/// <param name="EnableSecurityScan">When true, run security scan analysis steps.</param>
public sealed record AssemblyProfile(bool EnableDecompile, bool EnableSecurityScan);
