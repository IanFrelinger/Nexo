namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>Deterministic per-shot ballistics sample.</summary>
public readonly struct WeaponShotSample
{
    /// <summary>Creates a shot sample.</summary>
    public WeaponShotSample(
        uint shotSequence,
        int spreadYawMilliDegrees,
        int spreadPitchMilliDegrees,
        int recoilYawMilliDegrees,
        int recoilPitchMilliDegrees)
    {
        ShotSequence = shotSequence;
        SpreadYawMilliDegrees = spreadYawMilliDegrees;
        SpreadPitchMilliDegrees = spreadPitchMilliDegrees;
        RecoilYawMilliDegrees = recoilYawMilliDegrees;
        RecoilPitchMilliDegrees = recoilPitchMilliDegrees;
    }

    /// <summary>Monotonic shot id for this equipped weapon instance.</summary>
    public uint ShotSequence { get; }

    /// <summary>Yaw spread offset (millidegrees).</summary>
    public int SpreadYawMilliDegrees { get; }

    /// <summary>Pitch spread offset (millidegrees).</summary>
    public int SpreadPitchMilliDegrees { get; }

    /// <summary>Yaw recoil impulse (millidegrees).</summary>
    public int RecoilYawMilliDegrees { get; }

    /// <summary>Pitch recoil impulse (millidegrees).</summary>
    public int RecoilPitchMilliDegrees { get; }
}
