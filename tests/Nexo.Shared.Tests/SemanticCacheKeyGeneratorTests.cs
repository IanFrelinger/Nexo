using System.Collections.Generic;
using Xunit;
using Nexo.Shared;

namespace Nexo.Shared.Tests
{
    public class SemanticCacheKeyGeneratorTests
    {
        [Fact]
        public void Normalization_WhitespaceAndCase_ProducesSameKey()
        {
            var input1 = "  public void Foo() { return 1; }  ";
            var input2 = "public   void foo( ) {   return 1; }";
            var key1 = SemanticCacheKeyGenerator.Generate(input1);
            var key2 = SemanticCacheKeyGenerator.Generate(input2);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void DifferentContext_ProducesDifferentKeys()
        {
            var input = "code";
            var key1 = SemanticCacheKeyGenerator.Generate(input, new Dictionary<string, object> { { "user", "alice" } });
            var key2 = SemanticCacheKeyGenerator.Generate(input, new Dictionary<string, object> { { "user", "bob" } });
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void DifferentModelParameters_ProducesDifferentKeys()
        {
            var input = "code";
            var key1 = SemanticCacheKeyGenerator.Generate(input, null, new Dictionary<string, object> { { "model", "gpt-4" } });
            var key2 = SemanticCacheKeyGenerator.Generate(input, null, new Dictionary<string, object> { { "model", "llama2" } });
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void IdenticalInputsAndParameters_ProduceSameKey()
        {
            var input = "code";
            var context = new Dictionary<string, object> { { "user", "alice" } };
            var modelParams = new Dictionary<string, object> { { "model", "gpt-4" } };
            var key1 = SemanticCacheKeyGenerator.Generate(input, context, modelParams);
            var key2 = SemanticCacheKeyGenerator.Generate(input, context, modelParams);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void NullOrEmptyInput_ProducesValidKey()
        {
            var key1 = SemanticCacheKeyGenerator.Generate(null);
            var key2 = SemanticCacheKeyGenerator.Generate("");
            Assert.False(string.IsNullOrWhiteSpace(key1));
            Assert.False(string.IsNullOrWhiteSpace(key2));
        }
    }
} 