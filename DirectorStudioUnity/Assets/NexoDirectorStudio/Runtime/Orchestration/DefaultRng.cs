using System;

namespace NexoDirectorStudio.Orchestration
{
    public sealed class DefaultRng : IRng
    {
        private Random _r = new Random();
        public void Init(int seed){ _r = new Random(seed); UnityEngine.Random.InitState(seed); }
        public int Next() => _r.Next();
        public float Next01() => (float)_r.NextDouble();
    }
}
