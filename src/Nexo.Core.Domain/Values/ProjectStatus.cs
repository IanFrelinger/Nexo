using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents project status as a value object
    /// </summary>
    public sealed class ProjectStatus : IEquatable<ProjectStatus>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private ProjectStatus(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each project status
        public static readonly ProjectStatus NotInitialized = new("NotInitialized", "Project has not been initialized", 0);
        public static readonly ProjectStatus Planning = new("Planning", "Project is in planning phase", 1);
        public static readonly ProjectStatus Initialized = new("Initialized", "Project has been initialized", 2);
        public static readonly ProjectStatus Active = new("Active", "Project is actively being developed", 3);
        public static readonly ProjectStatus Running = new("Running", "Project is currently running", 4);
        public static readonly ProjectStatus Stopped = new("Stopped", "Project has been stopped", 5);
        public static readonly ProjectStatus OnHold = new("OnHold", "Project is temporarily on hold", 6);
        public static readonly ProjectStatus Failed = new("Failed", "Project has failed", 7);
        public static readonly ProjectStatus Completed = new("Completed", "Project has been completed", 8);
        public static readonly ProjectStatus Cancelled = new("Cancelled", "Project has been cancelled", 9);

        // Collection of all project statuses
        public static readonly IReadOnlyList<ProjectStatus> All = new[]
        {
            NotInitialized, Planning, Initialized, Active, Running, Stopped, OnHold, Failed, Completed, Cancelled
        };

        // Factory methods
        public static ProjectStatus FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Planning;

        public static ProjectStatus FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Planning;

        // Equality and comparison
        public bool Equals(ProjectStatus? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is ProjectStatus other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ProjectStatus? left, ProjectStatus? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(ProjectStatus? left, ProjectStatus? right) => 
            !(left == right);
    }
}
