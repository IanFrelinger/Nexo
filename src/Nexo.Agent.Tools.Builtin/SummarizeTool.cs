using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Agent.Abstractions;

namespace Nexo.Agent.Tools.Builtin;

/// <summary>
/// Tool for summarizing text content using heuristics or LLM.
/// </summary>
public class SummarizeTool : ITool<SummarizeInput, SummarizeOutput>
{
    public ToolManifest Manifest { get; }

    public SummarizeTool()
    {
        Manifest = ToolManifest.Create(
            id: "tool.text.summarize",
            name: "Text Summarize",
            description: "Summarize text content using heuristics or AI",
            requiredPermissions: ToolPermissions.None,
            capabilities: new[] { "text", "summarize", "ai", "nlp" },
            timeout: TimeSpan.FromMinutes(2)
        );
    }

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        var typedInput = JsonSerializer.Deserialize<SummarizeInput>(input) ?? throw new ArgumentException("Invalid input");
        var result = await ExecuteAsync(typedInput, context, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    public async Task<SummarizeOutput> ExecuteAsync(SummarizeInput input, ToolContext context, CancellationToken cancellationToken = default)
    {
        using var activity = new ActivitySource("Nexo.Tool").StartActivity("Summarize");
        activity?.SetTag("tool.summarize.max_length", input.MaxLength);
        activity?.SetTag("tool.summarize.text_length", input.Text.Length);

        try
        {
            context.Logger.LogInformation("Summarizing text (length: {Length}, max: {MaxLength})", input.Text.Length, input.MaxLength);

            string summary;
            var method = "heuristic";

            // For now, we'll use heuristic summarization
            // In a real implementation, this would check the agent mode and use LLM when available
            summary = await SummarizeHeuristicAsync(input.Text, input.MaxLength, context, cancellationToken);

            context.Logger.LogInformation("Generated summary using {Method} method (length: {Length})", method, summary.Length);
            activity?.SetTag("tool.summarize.method", method);
            activity?.SetTag("tool.summarize.summary_length", summary.Length);

            return new SummarizeOutput(true, summary, null, method, summary.Length);
        }
        catch (Exception ex)
        {
            var error = $"Error summarizing text: {ex.Message}";
            context.Logger.LogError(ex, error);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new SummarizeOutput(false, null, error, "error", 0);
        }
    }

    private async Task<string> SummarizeHeuristicAsync(string text, int maxLength, ToolContext context, CancellationToken cancellationToken)
    {
        // Simple heuristic summarization
        var sentences = SplitIntoSentences(text);
        
        if (sentences.Count == 0)
            return "No content to summarize.";

        if (sentences.Count == 1)
            return sentences[0];

        // Score sentences by length and position
        var scoredSentences = sentences.Select((sentence, index) => new
        {
            Sentence = sentence,
            Score = CalculateSentenceScore(sentence, index, sentences.Count)
        }).OrderByDescending(s => s.Score).ToList();

        // Select top sentences that fit within maxLength
        var selectedSentences = new List<string>();
        var currentLength = 0;

        foreach (var scored in scoredSentences)
        {
            if (currentLength + scored.Sentence.Length <= maxLength)
            {
                selectedSentences.Add(scored.Sentence);
                currentLength += scored.Sentence.Length;
            }
        }

        // If we didn't select any sentences, take the first one
        if (selectedSentences.Count == 0)
        {
            selectedSentences.Add(sentences[0]);
        }

        await Task.CompletedTask;
        return string.Join(" ", selectedSentences);
    }

    private static List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting
        var sentences = new List<string>();
        var current = "";

        foreach (var c in text)
        {
            current += c;
            if (c == '.' || c == '!' || c == '?')
            {
                var trimmed = current.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    sentences.Add(trimmed);
                }
                current = "";
            }
        }

        // Add remaining text as a sentence
        var remaining = current.Trim();
        if (!string.IsNullOrEmpty(remaining))
        {
            sentences.Add(remaining);
        }

        return sentences;
    }

    private static double CalculateSentenceScore(string sentence, int index, int totalSentences)
    {
        var score = 0.0;

        // Length score (prefer medium-length sentences)
        var length = sentence.Length;
        if (length > 20 && length < 100)
            score += 1.0;
        else if (length > 10 && length < 200)
            score += 0.5;

        // Position score (prefer first and last sentences)
        var position = (double)index / totalSentences;
        if (position < 0.1 || position > 0.9)
            score += 1.0;
        else if (position < 0.2 || position > 0.8)
            score += 0.5;

        // Keyword score (simple keyword detection)
        var keywords = new[] { "important", "key", "main", "primary", "significant", "critical", "essential" };
        var lowerSentence = sentence.ToLowerInvariant();
        foreach (var keyword in keywords)
        {
            if (lowerSentence.Contains(keyword))
            {
                score += 0.5;
            }
        }

        return score;
    }
}

/// <summary>
/// Input for the Summarize tool.
/// </summary>
/// <param name="Text">Text to summarize</param>
/// <param name="MaxLength">Maximum length of the summary</param>
/// <param name="Language">Language of the text (optional)</param>
public record SummarizeInput(
    string Text,
    int MaxLength = 200,
    string? Language = null);

/// <summary>
/// Output from the Summarize tool.
/// </summary>
/// <param name="Success">Whether the summarization was successful</param>
/// <param name="Summary">Generated summary</param>
/// <param name="Error">Error message if summarization failed</param>
/// <param name="Method">Method used for summarization</param>
/// <param name="SummaryLength">Length of the generated summary</param>
public record SummarizeOutput(
    bool Success,
    string? Summary,
    string? Error,
    string Method,
    int SummaryLength);
