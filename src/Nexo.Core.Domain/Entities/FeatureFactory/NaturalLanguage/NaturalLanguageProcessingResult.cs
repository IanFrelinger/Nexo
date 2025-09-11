using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.NaturalLanguage
{
    /// <summary>
    /// Represents the result of natural language processing
    /// </summary>
    public class NaturalLanguageProcessingResult
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string ProcessedText { get; set; } = string.Empty;
        public List<ExtractedEntity> Entities { get; set; } = new List<ExtractedEntity>();
        public List<ExtractedIntent> Intents { get; set; } = new List<ExtractedIntent>();
        public List<ExtractedKeyword> Keywords { get; set; } = new List<ExtractedKeyword>();
        public Dictionary<string, object> Sentiment { get; set; } = new Dictionary<string, object>();
        public double Confidence { get; set; }
        public string Language { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents an extracted entity from natural language
    /// </summary>
    public class ExtractedEntity
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents an extracted intent from natural language
    /// </summary>
    public class ExtractedIntent
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> Actions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents an extracted keyword from natural language
    /// </summary>
    public class ExtractedKeyword
    {
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
