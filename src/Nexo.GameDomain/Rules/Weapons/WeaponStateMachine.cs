using System;

namespace Nexo.GameDomain.Rules.Weapons;

/// <summary>
/// Deterministic tick-driven weapon state. No Unity, networking, animation,
/// or content-asset dependency.
/// </summary>
public sealed class WeaponStateMachine
{
    private uint _nextActionTick;
    private int _burstShotsRemaining;
    private uint _shotSequence;

    /// <summary>Creates a machine equipped with <paramref name="definition"/>.</summary>
    public WeaponStateMachine(WeaponCoreDefinition definition)
    {
        Equip(definition);
    }

    /// <summary>Current definition.</summary>
    public WeaponCoreDefinition Definition { get; private set; } = null!;

    /// <summary>Current phase.</summary>
    public WeaponPhase Phase { get; private set; }

    /// <summary>Rounds in magazine.</summary>
    public int MagazineAmmo { get; private set; }

    /// <summary>Reserve ammo pool.</summary>
    public int ReserveAmmo { get; private set; }

    /// <summary>Tick when reload completes.</summary>
    public uint ReloadCompletionTick { get; private set; }

    /// <summary>Advances reload/cooldown completion for <paramref name="tick"/>.</summary>
    public void Advance(uint tick)
    {
        if (Phase == WeaponPhase.Reloading &&
            tick >= ReloadCompletionTick)
        {
            var needed = Definition.MagazineCapacity - MagazineAmmo;
            var loaded = Math.Min(needed, ReserveAmmo);
            MagazineAmmo += loaded;
            ReserveAmmo -= loaded;
            Phase = WeaponPhase.Ready;
        }
        else if (Phase == WeaponPhase.Cooldown &&
                 tick >= _nextActionTick)
        {
            Phase = WeaponPhase.Ready;
        }
    }

    /// <summary>Attempts to fire at the given tick.</summary>
    public WeaponActionResult TryFire(
        uint tick,
        bool triggerPressed,
        bool triggerHeld,
        bool aimingDownSights,
        uint randomSeed)
    {
        Advance(tick);
        if (Phase == WeaponPhase.Disabled)
            return new WeaponActionResult(WeaponActionCode.Disabled);
        if (Phase == WeaponPhase.Reloading)
            return new WeaponActionResult(WeaponActionCode.Reloading);
        if (Phase == WeaponPhase.Cooldown)
            return new WeaponActionResult(WeaponActionCode.Cooldown);

        var triggerActive = Definition.FireMode switch
        {
            WeaponFireMode.SemiAutomatic => triggerPressed,
            WeaponFireMode.Automatic => triggerPressed || triggerHeld,
            WeaponFireMode.Burst =>
                triggerPressed || _burstShotsRemaining > 0,
            _ => false
        };
        if (!triggerActive)
            return new WeaponActionResult(WeaponActionCode.TriggerNotActive);
        if (MagazineAmmo <= 0)
            return new WeaponActionResult(WeaponActionCode.EmptyMagazine);

        if (Definition.FireMode == WeaponFireMode.Burst &&
            triggerPressed &&
            _burstShotsRemaining == 0)
        {
            _burstShotsRemaining = Definition.BurstCount;
        }

        MagazineAmmo--;
        _shotSequence++;
        if (_burstShotsRemaining > 0)
            _burstShotsRemaining--;

        var continuingBurst =
            Definition.FireMode == WeaponFireMode.Burst &&
            _burstShotsRemaining > 0;
        _nextActionTick = AddSaturating(
            tick,
            continuingBurst
                ? Definition.BurstIntervalTicks
                : Definition.FireIntervalTicks);
        Phase = WeaponPhase.Cooldown;

        return new WeaponActionResult(
            WeaponActionCode.Fired,
            WeaponBallistics.SampleShot(
                Definition,
                _shotSequence,
                randomSeed,
                aimingDownSights));
    }

    /// <summary>Attempts to start a reload at the given tick.</summary>
    public WeaponActionResult TryStartReload(uint tick)
    {
        Advance(tick);
        if (Phase == WeaponPhase.Disabled)
            return new WeaponActionResult(WeaponActionCode.Disabled);
        if (Phase == WeaponPhase.Reloading)
            return new WeaponActionResult(WeaponActionCode.Reloading);
        if (MagazineAmmo >= Definition.MagazineCapacity)
            return new WeaponActionResult(WeaponActionCode.MagazineFull);
        if (ReserveAmmo <= 0)
            return new WeaponActionResult(WeaponActionCode.NoReserveAmmo);

        Phase = WeaponPhase.Reloading;
        ReloadCompletionTick = AddSaturating(
            tick,
            Definition.ReloadDurationTicks);
        _burstShotsRemaining = 0;
        return new WeaponActionResult(WeaponActionCode.ReloadStarted);
    }

    /// <summary>Equips a definition and resets ammo/phase.</summary>
    public void Equip(WeaponCoreDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MagazineAmmo = definition.MagazineCapacity;
        ReserveAmmo = definition.StartingReserveAmmo;
        Phase = WeaponPhase.Ready;
        ReloadCompletionTick = 0;
        _nextActionTick = 0;
        _burstShotsRemaining = 0;
        _shotSequence = 0;
    }

    /// <summary>Disables the weapon until re-equipped.</summary>
    public void Disable()
    {
        Phase = WeaponPhase.Disabled;
        _burstShotsRemaining = 0;
    }

    private static uint AddSaturating(uint left, uint right)
        => uint.MaxValue - left < right ? uint.MaxValue : left + right;
}
