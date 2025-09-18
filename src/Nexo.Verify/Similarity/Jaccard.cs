using System.Linq;

namespace Nexo.Verify.Similarity
{
    /// <summary>
    /// Jaccard similarity implementation for text comparison
    /// </summary>
    public static class Jaccard
    {
        /// <summary>
        /// Calculate Jaccard similarity between two texts using token-based comparison
        /// </summary>
        /// <param name="textA">First text to compare</param>
        /// <param name="textB">Second text to compare</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        public static double Tokens(string textA, string textB)
        {
            if (string.IsNullOrEmpty(textA) && string.IsNullOrEmpty(textB))
                return 1.0;
                
            if (string.IsNullOrEmpty(textA) || string.IsNullOrEmpty(textB))
                return 0.0;

            var tokensA = Tokenize(textA);
            var tokensB = Tokenize(textB);
            
            var intersection = tokensA.Intersect(tokensB).Count();
            var union = tokensA.Union(tokensB).Count();
            
            return union == 0 ? 1.0 : (double)intersection / union;
        }

        /// <summary>
        /// Tokenize text into a set of normalized tokens
        /// </summary>
        private static HashSet<string> Tokenize(string text)
        {
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', ':' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length > 1) // Filter out single characters
                .ToHashSet();
        }
    }
}
