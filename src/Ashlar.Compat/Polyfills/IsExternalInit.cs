// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for init-only setters when targeting netstandard2.0.
/// The compiler emits references to this type for <c>init</c> properties and records.
/// </summary>
internal static class IsExternalInit
{
}

