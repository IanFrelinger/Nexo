using System;

namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Pure deterministic ballistics helpers.</summary>
public static class WeaponBallistics
{
    /// <summary>Evaluates damage after range falloff and hit-region multipliers.</summary>
    public static int EvaluateDamage(
        WeaponCoreDefinition definition,
        int distanceMillimeters,
        WeaponHitRegion region)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        var distance = Math.Max(0, distanceMillimeters);
        var falloffBasisPoints = 10_000;
        if (distance > definition.EffectiveRangeMillimeters)
        {
            var span =
                definition.MaximumRangeMillimeters -
                definition.EffectiveRangeMillimeters;
            var elapsed = Math.Min(
                span,
                distance - definition.EffectiveRangeMillimeters);
            var reduction =
                (10_000 - definition.MinimumDamageBasisPoints) *
                (long)elapsed /
                span;
            falloffBasisPoints = 10_000 - (int)reduction;
        }

        var regionBasisPoints = region switch
        {
            WeaponHitRegion.Head => definition.HeadMultiplierBasisPoints,
            WeaponHitRegion.Arm => definition.ArmMultiplierBasisPoints,
            WeaponHitRegion.Leg => definition.LegMultiplierBasisPoints,
            _ => 10_000
        };
        return (int)(
            definition.BaseDamage *
            (long)falloffBasisPoints *
            regionBasisPoints /
            100_000_000L);
    }

    /// <summary>
    /// Samples spread/recoil for a shot. ADS uses
    /// <see cref="WeaponCoreDefinition.AdsSpreadMilliDegrees"/>.
    /// </summary>
    public static WeaponShotSample SampleShot(
        WeaponCoreDefinition definition,
        uint shotSequence,
        uint randomSeed,
        bool aimingDownSights)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        var state = randomSeed ^ (shotSequence * 0x9E3779B9u);
        var spread = aimingDownSights
            ? definition.AdsSpreadMilliDegrees
            : definition.HipSpreadMilliDegrees;
        var spreadYaw = NextSigned(ref state, spread);
        var spreadPitch = NextSigned(ref state, spread);
        var recoilYaw = NextSigned(
            ref state,
            definition.HorizontalRecoilMilliDegrees);
        var recoilPitch =
            definition.VerticalRecoilMilliDegrees +
            Math.Abs(NextSigned(
                ref state,
                definition.VerticalRecoilMilliDegrees / 4));
        return new WeaponShotSample(
            shotSequence,
            spreadYaw,
            spreadPitch,
            recoilYaw,
            recoilPitch);
    }

    private static int NextSigned(ref uint state, int magnitude)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        if (magnitude <= 0)
            return 0;
        var range = (uint)(magnitude * 2 + 1);
        return (int)(state % range) - magnitude;
    }
}
