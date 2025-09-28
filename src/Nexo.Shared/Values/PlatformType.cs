using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Values
{
    /// <summary>
    /// Represents different platform types as a value object
    /// </summary>
    public sealed class PlatformType : IEquatable<PlatformType>
    {
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        private PlatformType(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        // Static instances for each platform type
        public static readonly PlatformType Windows = new("Windows", "Microsoft Windows operating system", 0);
        public static readonly PlatformType MacOS = new("macOS", "Apple macOS operating system", 1);
        public static readonly PlatformType Linux = new("Linux", "Linux operating system", 2);
        public static readonly PlatformType IOS = new("iOS", "Apple iOS mobile operating system", 3);
        public static readonly PlatformType Android = new("Android", "Google Android mobile operating system", 4);
        public static readonly PlatformType Web = new("Web", "Web-based platform", 5);
        public static readonly PlatformType Cloud = new("Cloud", "Cloud computing platform", 6);
        public static readonly PlatformType Container = new("Container", "Containerized platform", 7);
        public static readonly PlatformType Desktop = new("Desktop", "Desktop application platform", 8);
        public static readonly PlatformType Mobile = new("Mobile", "Mobile application platform", 9);
        public static readonly PlatformType CrossPlatform = new("CrossPlatform", "Cross-platform solution", 10);
        public static readonly PlatformType Unknown = new("Unknown", "Unknown or unsupported platform", 99);

        // Collection of all platform types
        public static readonly IReadOnlyList<PlatformType> All = new[]
        {
            Windows, MacOS, Linux, IOS, Android, Web, Cloud, Container, Desktop, Mobile, CrossPlatform, Unknown
        };

        // Factory methods
        public static PlatformType FromName(string name) => 
            All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Unknown;

        public static PlatformType FromValue(int value) => 
            All.FirstOrDefault(p => p.Value == value) ?? Unknown;

        // Equality and comparison
        public bool Equals(PlatformType? other) => other != null && Value == other.Value;
        public override bool Equals(object? obj) => obj is PlatformType other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(PlatformType? left, PlatformType? right) => 
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
        public static bool operator !=(PlatformType? left, PlatformType? right) => 
            !(left == right);
    }
}
