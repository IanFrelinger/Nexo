using System;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for required members support when targeting netstandard2.0.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
        public SetsRequiredMembersAttribute() { }
    }
}
