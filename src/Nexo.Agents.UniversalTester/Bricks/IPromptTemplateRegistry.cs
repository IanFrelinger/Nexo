namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Named prompt templates for bricks. Enables A/B testing and game-specific prompts without code changes.
/// </summary>
public interface IPromptTemplateRegistry
{
    /// <summary>
    /// Gets a named template if registered.
    /// </summary>
    /// <param name="name">Template key (e.g. "temporal-gameplay", "understanding-default").</param>
    /// <returns>The template, or null if not found.</returns>
    PromptTemplate? TryGetTemplate(string name);
}

/// <summary>
/// A prompt template with optional system and user parts. Placeholders use {{key}} syntax.
/// </summary>
public sealed record PromptTemplate
{
    /// <summary>System prompt for the model (e.g. role description).</summary>
    public string? SystemPrompt { get; init; }
    /// <summary>User prompt template with {{placeholder}} substitution.</summary>
    public string? UserPromptTemplate { get; init; }

    /// <summary>
    /// Substitutes {{key}} placeholders in a string.
    /// </summary>
    public static string Substitute(string template, IReadOnlyDictionary<string, string> vars)
    {
        if (string.IsNullOrEmpty(template) || vars == null || vars.Count == 0)
            return template;
        var result = template;
        foreach (var (k, v) in vars)
        {
            var placeholder = "{{" + k + "}}";
            result = result.Replace(placeholder, v ?? "", StringComparison.Ordinal);
        }
        return result;
    }
}
