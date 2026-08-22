using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Contracts;
/// <summary>
/// Request to generate a new game descriptor via the Forge AI pipeline.
/// </summary>
/// <param name="Prompt">Natural-language description of the desired game element.</param>
/// <param name="Category">Optional category hint (e.g. <c>"weapon"</c>, <c>"ability"</c>, <c>"map_element"</c>).</param>
public sealed record ForgeGenerateRequest(string Prompt, string? Category);
