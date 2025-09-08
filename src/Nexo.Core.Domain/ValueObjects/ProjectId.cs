using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class ProjectId : IComparable<ProjectId>
    {
        public Guid Value { get; }
        public ProjectId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Project ID cannot be empty", nameof(value));
            Value = value;
        }
        public static ProjectId New() { return new ProjectId(Guid.NewGuid()); }
        public static ProjectId Parse(string value) { return new ProjectId(Guid.Parse(value)); }
        public static bool TryParse(string value, out ProjectId? projectId)
        {
            projectId = null;
            Guid guid;
            if (Guid.TryParse(value, out guid) && guid != Guid.Empty)
            {
                projectId = new ProjectId(guid);
                return true;
            }
            return false;
        }
        public static implicit operator Guid(ProjectId id) { return id.Value; }
        public override string ToString() { return Value.ToString(); }
        public int CompareTo(ProjectId? other) { return other == null ? 1 : Value.CompareTo(other.Value); }
    }
}