using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Services.RAG
{
    /// <summary>
    /// Parser for Markdown documentation files
    /// </summary>
    public class MarkdownDocumentationParser : IDocumentationParser
    {
        private readonly ILogger<MarkdownDocumentationParser> _logger;

        public MarkdownDocumentationParser(ILogger<MarkdownDocumentationParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<DocumentationChunk>> ParseDocumentationAsync(string content, DocumentationMetadata metadata)
        {
            try
            {
                var chunks = new List<DocumentationChunk>();

                // Split content into sections based on headers
                var sections = SplitIntoSections(content);

                foreach (var section in sections)
                {
                    var chunk = CreateDocumentationChunk(section, metadata);
                    if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }

                _logger.LogDebug("Parsed {Count} documentation chunks from content", chunks.Count);
                return await Task.FromResult(chunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing documentation content");
                throw;
            }
        }

        private List<DocumentationSection> SplitIntoSections(string content)
        {
            var sections = new List<DocumentationSection>();
            var lines = content.Split('\n');
            var currentSection = new DocumentationSection();
            var currentContent = new List<string>();

            foreach (var line in lines)
            {
                // Check if this is a header
                if (IsHeader(line))
                {
                    // Save previous section if it has content
                    if (currentContent.Any())
                    {
                        currentSection.Content = string.Join("\n", currentContent);
                        sections.Add(currentSection);
                    }

                    // Start new section
                    currentSection = new DocumentationSection
                    {
                        Title = ExtractHeaderText(line),
                        Level = GetHeaderLevel(line)
                    };
                    currentContent = new List<string>();
                }
                else
                {
                    currentContent.Add(line);
                }
            }

            // Add the last section
            if (currentContent.Any())
            {
                currentSection.Content = string.Join("\n", currentContent);
                sections.Add(currentSection);
            }

            return sections;
        }

        private bool IsHeader(string line)
        {
            return line.TrimStart().StartsWith("#");
        }

        private string ExtractHeaderText(string line)
        {
            return line.TrimStart('#').Trim();
        }

        private int GetHeaderLevel(string line)
        {
            var trimmed = line.TrimStart();
            var level = 0;
            while (level < trimmed.Length && trimmed[level] == '#')
            {
                level++;
            }
            return level;
        }

        private DocumentationChunk? CreateDocumentationChunk(DocumentationSection section, DocumentationMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(section.Content))
                return null;

            // Clean up the content
            var cleanContent = CleanContent(section.Content);

            // Skip very short sections
            if (cleanContent.Length < 50)
                return null;

            // Extract code blocks and examples
            var codeBlocks = ExtractCodeBlocks(cleanContent);
            var examples = ExtractExamples(cleanContent);

            // Generate tags based on content
            var tags = GenerateTags(cleanContent, metadata);

            return new DocumentationChunk
            {
                Id = Guid.NewGuid().ToString(),
                Title = section.Title,
                Content = cleanContent,
                DocumentationType = metadata.DocumentationType,
                Version = metadata.Version,
                Runtime = metadata.Runtime,
                Tags = tags.ToList(),
                Categories = metadata.Categories.ToList(),
                SourceUrl = metadata.Source,
                IndexedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["HeaderLevel"] = section.Level,
                    ["CodeBlocks"] = codeBlocks,
                    ["Examples"] = examples,
                    ["WordCount"] = cleanContent.Split(' ').Length
                }
            };
        }

        private string CleanContent(string content)
        {
            // Remove excessive whitespace
            content = Regex.Replace(content, @"\s+", " ");
            
            // Remove markdown formatting but keep structure
            content = Regex.Replace(content, @"\*\*(.*?)\*\*", "$1"); // Bold
            content = Regex.Replace(content, @"\*(.*?)\*", "$1"); // Italic
            content = Regex.Replace(content, @"`(.*?)`", "$1"); // Inline code
            content = Regex.Replace(content, @"\[([^\]]+)\]\([^)]+\)", "$1"); // Links
            
            return content.Trim();
        }

        private List<string> ExtractCodeBlocks(string content)
        {
            var codeBlocks = new List<string>();
            var matches = Regex.Matches(content, @"```(\w+)?\n(.*?)```", RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                var language = match.Groups[1].Value;
                var code = match.Groups[2].Value.Trim();
                
                if (!string.IsNullOrEmpty(code))
                {
                    codeBlocks.Add($"[{language}]{code}");
                }
            }

            return codeBlocks;
        }

        private List<string> ExtractExamples(string content)
        {
            var examples = new List<string>();
            
            // Look for example patterns
            var exampleMatches = Regex.Matches(content, @"(?:Example|Example:|Examples?)[\s\S]*?(?=\n\n|\n#|$)", RegexOptions.IgnoreCase);
            
            foreach (Match match in exampleMatches)
            {
                var example = match.Value.Trim();
                if (example.Length > 20)
                {
                    examples.Add(example);
                }
            }

            return examples;
        }

        private List<string> GenerateTags(string content, DocumentationMetadata metadata)
        {
            var tags = new List<string>();

            // Add metadata tags
            tags.AddRange(metadata.Tags);
            tags.AddRange(metadata.Categories);

            // Add content-based tags
            var contentLower = content.ToLowerInvariant();

            // C# language features
            if (contentLower.Contains("async") || contentLower.Contains("await"))
                tags.Add("async-await");
            if (contentLower.Contains("linq"))
                tags.Add("linq");
            if (contentLower.Contains("lambda"))
                tags.Add("lambda");
            if (contentLower.Contains("delegate"))
                tags.Add("delegate");
            if (contentLower.Contains("event"))
                tags.Add("event");
            if (contentLower.Contains("property"))
                tags.Add("property");
            if (contentLower.Contains("method"))
                tags.Add("method");
            if (contentLower.Contains("class"))
                tags.Add("class");
            if (contentLower.Contains("interface"))
                tags.Add("interface");
            if (contentLower.Contains("namespace"))
                tags.Add("namespace");

            // .NET concepts
            if (contentLower.Contains("dependency injection"))
                tags.Add("dependency-injection");
            if (contentLower.Contains("middleware"))
                tags.Add("middleware");
            if (contentLower.Contains("configuration"))
                tags.Add("configuration");
            if (contentLower.Contains("logging"))
                tags.Add("logging");
            if (contentLower.Contains("performance"))
                tags.Add("performance");
            if (contentLower.Contains("memory"))
                tags.Add("memory");
            if (contentLower.Contains("threading"))
                tags.Add("threading");

            // Framework-specific
            if (contentLower.Contains("asp.net") || contentLower.Contains("aspnet"))
                tags.Add("aspnet");
            if (contentLower.Contains("mvc"))
                tags.Add("mvc");
            if (contentLower.Contains("web api"))
                tags.Add("webapi");
            if (contentLower.Contains("entity framework"))
                tags.Add("entity-framework");

            return tags.Distinct().ToList();
        }

        private class DocumentationSection
        {
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public int Level { get; set; }
        }
    }
}
