using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;

/// <summary>
/// Response from the Forge AI generation pipeline.
/// </summary>
/// <param name="Category">Resolved category of the generated descriptor.</param>
/// <param name="DescriptorJson">JSON-serialised descriptor payload.</param>
/// <param name="Error">Error message if generation failed; <c>null</c> on success.</param>
public sealed record ForgeGenerateResponse(string Category, string DescriptorJson, string? Error);
