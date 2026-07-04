using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Output from an LLM model after text generation.
///
/// Contains the generated text from the model.
/// Returned by IModel.CompleteAsync after LLM processing.
/// </summary>
/// <param name="Text">The generated text output from the model.</param>
public sealed record ModelOutput(string Text);
