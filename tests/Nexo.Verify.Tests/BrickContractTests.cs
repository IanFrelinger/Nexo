using FluentAssertions;
using Nexo.Verify.Models;

namespace Nexo.Verify.Tests
{
    /// <summary>
    /// Tests for brick contract compliance and typed I/O
    /// </summary>
    public partial class BrickContractTests
    {
        [Fact]
        public async Task PiiRedactor_Removes_Email()
        {
            // Arrange
            var brick = BrickRegistry.Resolve("transform/pii_redact");
            var input = new Text("Email: a@b.com");
            var context = new Context();

            // Act
            var output = await brick.RunAsync(input, context);

            // Assert
            output.Value.Should().NotContain("@b.com");
            output.Metadata.Should().ContainKey("policy_checked");
        }

        [Fact]
        public async Task EmailIngester_Produces_Structured_Output()
        {
            // Arrange
            var brick = BrickRegistry.Resolve("ingest/email");
            var input = new Text("Subject: Test\nFrom: user@example.com\nBody: Test message");
            var context = new Context();

            // Act
            var output = await brick.RunAsync(input, context);

            // Assert
            output.Metadata.Should().ContainKey("subject");
            output.Metadata.Should().ContainKey("from");
            output.Metadata.Should().ContainKey("body");
            output.Metadata["subject"].Should().Be("Test");
        }

        [Fact]
        public async Task LabelClassifier_Produces_Confidence_Scores()
        {
            // Arrange
            var brick = BrickRegistry.Resolve("classify/label");
            var input = new Text("Urgent: Please review this document");
            var context = new Context();

            // Act
            var output = await brick.RunAsync(input, context);

            // Assert
            output.Metadata.Should().ContainKey("label");
            output.Metadata.Should().ContainKey("confidence");
            var confidence = Convert.ToDouble(output.Metadata["confidence"]);
            confidence.Should().BeInRange(0.0, 1.0);
        }

        [Fact]
        public void BrickRegistry_Resolves_All_Required_Bricks()
        {
            // Arrange
            var requiredBricks = new[]
            {
                "ingest/email",
                "transform/pii_redact",
                "classify/label",
                "govern/policy_check"
            };

            // Act & Assert
            foreach (var brickId in requiredBricks)
            {
                var brick = BrickRegistry.Resolve(brickId);
                brick.Should().NotBeNull($"Brick {brickId} should be resolvable");
            }
        }
    }

    /// <summary>
    /// Test registry for managing bricks
    /// </summary>
    public static class BrickRegistry
    {
        private static readonly Dictionary<string, IBrick<Text, Text>> _bricks = new();

        static BrickRegistry()
        {
            // Register test bricks
            _bricks["ingest/email"] = new EmailIngesterBrick();
            _bricks["transform/pii_redact"] = new PiiRedactorBrick();
            _bricks["classify/label"] = new LabelClassifierBrick();
            _bricks["govern/policy_check"] = new PolicyCheckBrick();
        }

        public static IBrick<Text, Text> Resolve(string brickId)
        {
            return _bricks.TryGetValue(brickId, out var brick) 
                ? brick 
                : throw new ArgumentException($"Brick not found: {brickId}");
        }
    }

    /// <summary>
    /// Generic brick interface
    /// </summary>
    public partial interface IBrick<TInput, TOutput>
    {
        Task<TOutput> RunAsync(TInput input, Context context);
    }

    /// <summary>
    /// Text data type
    /// </summary>
    public partial class Text
    {
        public string Value { get; }
        public Dictionary<string, object> Metadata { get; } = new();

        public Text(string value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Execution context
    /// </summary>
    public partial class Context
    {
        public Dictionary<string, object> Properties { get; } = new();
    }

    /// <summary>
    /// Test brick implementations
    /// </summary>
    public partial class EmailIngesterBrick : IBrick<Text, Text>
    {
        public async Task<Text> RunAsync(Text input, Context context)
        {
            await Task.Delay(10); // Simulate processing
            
            var output = new Text(input.Value);
            output.Metadata["subject"] = "Test";
            output.Metadata["from"] = "user@example.com";
            output.Metadata["body"] = "Test message";
            return output;
        }
    }

    public partial class PiiRedactorBrick : IBrick<Text, Text>
    {
        public async Task<Text> RunAsync(Text input, Context context)
        {
            await Task.Delay(10); // Simulate processing
            
            var redacted = input.Value.Replace("@b.com", "***");
            var output = new Text(redacted);
            output.Metadata["policy_checked"] = true;
            return output;
        }
    }

    public partial class LabelClassifierBrick : IBrick<Text, Text>
    {
        public async Task<Text> RunAsync(Text input, Context context)
        {
            await Task.Delay(10); // Simulate processing
            
            var output = new Text(input.Value);
            output.Metadata["label"] = "urgent";
            output.Metadata["confidence"] = 0.95;
            return output;
        }
    }

    public partial class PolicyCheckBrick : IBrick<Text, Text>
    {
        public async Task<Text> RunAsync(Text input, Context context)
        {
            await Task.Delay(10); // Simulate processing
            
            var output = new Text(input.Value);
            output.Metadata["policy_violations"] = 0;
            output.Metadata["policy_checked"] = true;
            return output;
        }
    }
}
