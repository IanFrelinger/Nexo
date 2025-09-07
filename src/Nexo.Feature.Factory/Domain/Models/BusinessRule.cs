using System;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents a business rule that needs to be implemented.
    /// </summary>
    public sealed class BusinessRule
    {
        /// <summary>
        /// Gets the name of the business rule.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the business rule.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the condition that defines when this rule applies.
        /// </summary>
        public string Condition { get; }

        /// <summary>
        /// Gets the action to take when the rule is violated.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Gets the priority of the business rule.
        /// </summary>
        public BusinessRulePriority Priority { get; }

        /// <summary>
        /// Gets the entity or value object this rule applies to.
        /// </summary>
        public string? AppliesTo { get; }

        /// <summary>
        /// Initializes a new instance of the BusinessRule class.
        /// </summary>
        /// <param name="name">The rule name</param>
        /// <param name="description">The rule description</param>
        /// <param name="condition">The rule condition</param>
        /// <param name="action">The rule action</param>
        /// <param name="priority">The rule priority</param>
        /// <param name="appliesTo">The entity/value object this rule applies to</param>
        public BusinessRule(string name, string description, string condition, string action, BusinessRulePriority priority = BusinessRulePriority.Medium, string? appliesTo = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Priority = priority;
            AppliesTo = appliesTo;
        }
    }

    /// <summary>
    /// Represents the priority of a business rule.
    /// </summary>
    public enum BusinessRulePriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
