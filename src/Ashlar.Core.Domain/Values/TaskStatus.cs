using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashlar.Core.Domain.Values
{
    /// <summary>
    /// Defines various statuses that a task can have, indicating its current state in the workflow as a value object.
    /// 
    /// Provides predefined status values:
    /// - Todo: Task is yet to be started or worked on
    /// - InProgress: Task is currently being worked on
    /// - Done: Task has been completed and meets all criteria
    /// - Blocked: Task is blocked and cannot proceed
    /// 
    /// Implements IEquatable for value-based equality comparison.
    /// Provides factory methods FromName and FromValue for parsing.
    /// </summary>
    public sealed class TaskStatus : IEquatable<TaskStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private TaskStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each task status
        public static readonly TaskStatus Todo = new("Todo", "Task is yet to be started or worked on", 0);
        public static readonly TaskStatus InProgress = new("InProgress", "Task is currently being worked on", 1);
        public static readonly TaskStatus Done = new("Done", "Task has been completed and meets all criteria", 2);
        public static readonly TaskStatus Blocked = new("Blocked", "Task is blocked and cannot proceed", 3);

        // Collection of all task statuses
        public static readonly IReadOnlyList<TaskStatus> All = new[]
        {
            Todo, InProgress, Done, Blocked
        };

        // Factory methods
        public static TaskStatus FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Todo;

        public static TaskStatus FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Todo;

        // Equality and comparison
        public bool Equals(TaskStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is TaskStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(TaskStatus? left, TaskStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(TaskStatus? left, TaskStatus? right) => 
            !(left == right);
    }
}
