namespace Ashlar.Commercial.GameDomain.Aesthetics;
/// <summary>
/// A single validation finding for an <see cref="AestheticPack"/> or binding.
/// </summary>
public readonly record struct AestheticValidationIssue(string Code, string Message);
