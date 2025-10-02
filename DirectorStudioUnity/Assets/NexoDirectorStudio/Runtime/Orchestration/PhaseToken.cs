using System;

namespace NexoDirectorStudio.Orchestration
{
    [Serializable]
    public sealed class PhaseToken : IEquatable<PhaseToken>
    {
        public string Value;
        public PhaseToken(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as PhaseToken);
        public bool Equals(PhaseToken other) => other != null && other.Value == Value;
        public static implicit operator string(PhaseToken t) => t.Value;
        public static PhaseToken Of(string v) => new PhaseToken(v);
    }
}
