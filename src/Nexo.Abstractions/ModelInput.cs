using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Input to an LLM model for text generation.
///
/// Contains a list of messages with roles (e.g., "system", "user", "assistant")
/// and content. Used by IModel.CompleteAsync for LLM interactions.
/// </summary>
/// <param name="Messages">List of messages, each with a role and content string.</param>
public sealed record ModelInput(IReadOnlyList<(string role, string content)> Messages);
