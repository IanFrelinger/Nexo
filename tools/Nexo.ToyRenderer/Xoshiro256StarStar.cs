namespace Nexo.ToyRenderer;

/// <summary>Explicit xoshiro256** PRNG for cross-version deterministic simulation.</summary>
public sealed class Xoshiro256StarStar
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public Xoshiro256StarStar(ulong seed)
    {
        var split = SplitMix64(seed);
        _s0 = split();
        _s1 = split();
        _s2 = split();
        _s3 = split();
    }

    public ulong NextULong()
    {
        var result = Rotl(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 45);

        return result;
    }

    public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    private Func<ulong> SplitMix64(ulong seed)
    {
        var state = seed;
        return () =>
        {
            state += 0x9E3779B97F4A7C15UL;
            var z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        };
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));
}
