namespace Ashlar.Core.Application.Generation;

/// <summary>
/// One validator failure fed back into the next draft attempt.
/// Domain-neutral: reason is an opaque string from <c>IPostValidator</c>.
/// </summary>
/// <param name="Source">Validator identity (typically type name).</param>
/// <param name="Reason">Human/model-readable failure reason.</param>
public sealed record ValidationFeedback(string Source, string Reason);
