using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashlar.Core.Domain.Values
{
    /// <summary>
    /// Represents AI provider type as a value object.
    /// 
    /// Provides predefined AI provider type values:
    /// - Mock: Mock provider for testing
    /// - LlamaWebAssembly: Llama running in WebAssembly
    /// - LlamaNative: Native Llama implementation
    /// - Llama: Generic Llama provider
    /// - Ollama: Ollama provider
    /// - OpenAI: OpenAI provider
    /// - Anthropic: Anthropic provider
    /// - AzureOpenAI: Azure OpenAI provider
    /// - GoogleAI: Google AI provider
    /// - HuggingFace: HuggingFace provider
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// Used to identify the AI provider being used for model operations.
    /// </summary>
    public sealed class AIProviderType : IEquatable<AIProviderType>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private AIProviderType(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each aiprovidertype
        public static readonly AIProviderType Mock = new("Mock", "Description for Mock", 0);
        public static readonly AIProviderType LlamaWebAssembly = new("LlamaWebAssembly", "Description for LlamaWebAssembly", 1);
        public static readonly AIProviderType LlamaNative = new("LlamaNative", "Description for LlamaNative", 2);
        public static readonly AIProviderType Llama = new("Llama", "Description for Llama", 3);
        public static readonly AIProviderType Ollama = new("Ollama", "Description for Ollama", 4);
        public static readonly AIProviderType OpenAI = new("OpenAI", "Description for OpenAI", 5);
        public static readonly AIProviderType Anthropic = new("Anthropic", "Description for Anthropic", 6);
        public static readonly AIProviderType AzureOpenAI = new("AzureOpenAI", "Description for AzureOpenAI", 7);
        public static readonly AIProviderType GoogleAI = new("GoogleAI", "Description for GoogleAI", 8);
        public static readonly AIProviderType HuggingFace = new("HuggingFace", "Description for HuggingFace", 9);

        // Collection of all aiprovidertypes
        public static readonly IReadOnlyList<AIProviderType> All = new[]
        {
            Mock,
            LlamaWebAssembly,
            LlamaNative,
            Llama,
            Ollama,
            OpenAI,
            Anthropic,
            AzureOpenAI,
            GoogleAI,
            HuggingFace,
        };

        // Factory methods
        public static AIProviderType FromName(string name) => 
            All.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Mock;

        public static AIProviderType FromValue(int value) => 
            All.FirstOrDefault(v => v.Value == value) ?? Mock;

        // Equality and comparison
        public bool Equals(AIProviderType? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is AIProviderType other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(AIProviderType? left, AIProviderType? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(AIProviderType? left, AIProviderType? right) => 
            !(left == right);
    }
}