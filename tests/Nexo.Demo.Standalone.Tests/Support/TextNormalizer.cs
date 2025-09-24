using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Nexo.Demo.Tests.Support
{
    /// <summary>
    /// Provides text normalization utilities for demo scenarios
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// Normalizes text for comparison by removing extra whitespace and standardizing format
        /// </summary>
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            // Remove extra whitespace and normalize line endings
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Calculates Jaccard similarity between two text strings
        /// </summary>
        public static double JaccardTokens(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            var tokensA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var tokensB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            var intersection = tokensA.Intersect(tokensB).Count();
            var union = tokensA.Union(tokensB).Count();

            return union == 0 ? 0.0 : (double)intersection / union;
        }
    }
}
