using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Interface for LLM models used for AI operations.
///
/// Models take ModelInput (messages) and return ModelOutput (text).
/// Used by agents for reasoning and decision-making.
/// </summary>
public interface IModel
{
    /// <summary>
    /// Generates a completion from the supplied message history.
    /// </summary>
    /// <param name="input">Conversation messages with roles and content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated text output from the model.</returns>
    Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct);
}
