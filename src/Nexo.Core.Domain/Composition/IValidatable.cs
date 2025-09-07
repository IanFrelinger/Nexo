using System.Collections.Generic;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Interface for objects that can be validated using a set of validation rules.
    /// This provides consistent validation patterns across the domain model.
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Validates this object using its validation rules.
        /// </summary>
        /// <returns>A validation result containing any validation errors or warnings</returns>
        ValidationResult Validate();
        
        /// <summary>
        /// Gets all validation rules associated with this object.
        /// </summary>
        /// <returns>An enumerable of validation rules</returns>
        IEnumerable<ValidationRule> GetValidationRules();
    }
} 