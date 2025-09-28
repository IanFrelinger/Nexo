using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents the priority level of a task as a value object
    /// </summary>
    public sealed class TaskPriority : IEquatable<TaskPriority>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private TaskPriority(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each task priority
        public static readonly TaskPriority Low = new("Low", "Low priority task", 1);
        public static readonly TaskPriority Medium = new("Medium", "Medium priority task", 2);
        public static readonly TaskPriority High = new("High", "High priority task", 3);
        public static readonly TaskPriority Critical = new("Critical", "Critical priority task", 4);
        public static readonly TaskPriority Urgent = new("Urgent", "Urgent priority task", 5);

        // Collection of all task priorities
        public static readonly IReadOnlyList<TaskPriority> All = new[]
        {
            Low, Medium, High, Critical, Urgent
        };

        // Factory methods
        public static TaskPriority FromName(string name) => 
            All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Medium;

        public static TaskPriority FromValue(int value) => 
            All.FirstOrDefault(p => p.Value == value) ?? Medium;

        // Equality and comparison
        public bool Equals(TaskPriority? other) => 
            other != null && Value == other.Value;

        public override bool Equals(object? obj) => 
            obj is TaskPriority other && Equals(other);

        public override int GetHashCode() => 
            Value.GetHashCode();

        public override string ToString() => Name;

        public static bool operator ==(TaskPriority? left, TaskPriority? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        public static bool operator !=(TaskPriority? left, TaskPriority? right) => 
            !(left == right);

        public static bool operator >(TaskPriority left, TaskPriority right) => 
            left.Value > right.Value;

        public static bool operator <(TaskPriority left, TaskPriority right) => 
            left.Value < right.Value;

        public static bool operator >=(TaskPriority left, TaskPriority right) => 
            left.Value >= right.Value;

        public static bool operator <=(TaskPriority left, TaskPriority right) => 
            left.Value <= right.Value;
    }
}