using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.ValueObjects
{
    public sealed class ContainerRuntime : IComparable<ContainerRuntime>
    {
        private static readonly HashSet<string> ValidRuntimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "docker", "podman", "containerd", "none"
        };
        public string Value { get; }
        public ContainerRuntime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Container runtime cannot be empty", nameof(value));
            var normalized = value.ToLowerInvariant();
            if (!ValidRuntimes.Contains(normalized))
                throw new ArgumentException("Invalid container runtime: " + value + ". Valid values are: " + string.Join(", ", ValidRuntimes), nameof(value));
            Value = normalized;
        }
        public static ContainerRuntime Docker { get { return new ContainerRuntime("docker"); } }
        public static ContainerRuntime Podman { get { return new ContainerRuntime("podman"); } }
        public static ContainerRuntime Containerd { get { return new ContainerRuntime("containerd"); } }
        public static ContainerRuntime None { get { return new ContainerRuntime("none"); } }
        public bool IsAvailable { get { return Value != "none"; } }
        public static implicit operator string(ContainerRuntime runtime) { return runtime.Value; }
        public override string ToString() { return Value; }
        public int CompareTo(ContainerRuntime other) { return string.Compare(Value, other == null ? null : other.Value, StringComparison.Ordinal); }
    }
} 