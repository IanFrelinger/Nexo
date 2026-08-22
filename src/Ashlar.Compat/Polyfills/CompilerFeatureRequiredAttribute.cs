using System;

// Polyfills for C# 11 required members when targeting netstandard2.0.
namespace System.Runtime.CompilerServices
{

    /// <summary>
    /// Polyfill used by the compiler to mark required-member support.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
    }
}
