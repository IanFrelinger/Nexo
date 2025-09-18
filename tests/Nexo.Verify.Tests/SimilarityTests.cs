using FluentAssertions;
using Nexo.Verify.Similarity;

namespace Nexo.Verify.Tests
{
    /// <summary>
    /// Tests for Jaccard similarity calculations
    /// </summary>
    public class SimilarityTests
    {
        [Fact]
        public void Jaccard_Identical_Texts_Returns_One()
        {
            // Arrange
            var text = "This is a test message with some content";

            // Act
            var similarity = Jaccard.Tokens(text, text);

            // Assert
            similarity.Should().Be(1.0);
        }

        [Fact]
        public void Jaccard_Completely_Different_Texts_Returns_Zero()
        {
            // Arrange
            var text1 = "This is completely different";
            var text2 = "That is totally unrelated";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().Be(0.0);
        }

        [Fact]
        public void Jaccard_Partially_Similar_Texts_Returns_Reasonable_Score()
        {
            // Arrange
            var text1 = "The quick brown fox jumps over the lazy dog";
            var text2 = "A quick brown fox jumps over a lazy dog";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().BeGreaterThan(0.5);
            similarity.Should().BeLessThan(1.0);
        }

        [Fact]
        public void Jaccard_Empty_Texts_Returns_One()
        {
            // Arrange
            var text1 = "";
            var text2 = "";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().Be(1.0);
        }

        [Fact]
        public void Jaccard_One_Empty_Text_Returns_Zero()
        {
            // Arrange
            var text1 = "Some content";
            var text2 = "";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().Be(0.0);
        }

        [Fact]
        public void Jaccard_Case_Insensitive()
        {
            // Arrange
            var text1 = "The Quick Brown Fox";
            var text2 = "the quick brown fox";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().Be(1.0);
        }

        [Fact]
        public void Jaccard_Handles_Punctuation()
        {
            // Arrange
            var text1 = "Hello, world! How are you?";
            var text2 = "Hello world How are you";

            // Act
            var similarity = Jaccard.Tokens(text1, text2);

            // Assert
            similarity.Should().BeGreaterThan(0.8);
        }
    }
}
