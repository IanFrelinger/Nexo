using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Core interface for composable objects that can be combined with other objects of the same type.
    /// This enables flexible composition patterns throughout the domain model.
    /// </summary>
    /// <typeparam name="T">The type that can be composed with itself</typeparam>
    public interface IComposable<T>
    {
        /// <summary>
        /// Composes this object with other objects of the same type.
        /// </summary>
        /// <param name="components">The components to compose with this object</param>
        /// <returns>A new composed object that combines all components</returns>
        T Compose(params T[] components);
        
        /// <summary>
        /// Determines if this object can be composed with another object.
        /// </summary>
        /// <param name="other">The other object to check composition compatibility with</param>
        /// <returns>True if composition is possible, false otherwise</returns>
        bool CanComposeWith(T other);
        
        /// <summary>
        /// Decomposes this object into its constituent parts.
        /// </summary>
        /// <returns>An enumerable of the decomposed components</returns>
        IEnumerable<T> Decompose();
    }
} 