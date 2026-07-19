using FluentAssertions;
using Nexo.GameDomain.Rules.Weapons;
using Xunit;

namespace Nexo.Tests.GameDomain;

public sealed class WeaponRulesTests
{
    [Fact]
    public void AimDownSights_ReducesSpreadForSameShotSeed()
    {
        var definition = AutomaticRifle();
        var hip = WeaponBallistics.SampleShot(
            definition,
            shotSequence: 7,
            randomSeed: 1234,
            aimingDownSights: false);
        var ads = WeaponBallistics.SampleShot(
            definition,
            shotSequence: 7,
            randomSeed: 1234,
            aimingDownSights: true);

        Math.Abs(ads.SpreadYawMilliDegrees)
            .Should()
            .BeLessThan(Math.Abs(hip.SpreadYawMilliDegrees));
        Math.Abs(ads.SpreadPitchMilliDegrees)
            .Should()
            .BeLessThan(Math.Abs(hip.SpreadPitchMilliDegrees));
    }

    [Fact]
    public void TryFire_ConsumesMagazineAndEntersCooldown()
    {
        var machine = new WeaponStateMachine(AutomaticRifle());
        var result = machine.TryFire(
            tick: 1,
            triggerPressed: true,
            triggerHeld: false,
            aimingDownSights: false,
            randomSeed: 1);

        result.Code.Should().Be(WeaponActionCode.Fired);
        machine.MagazineAmmo.Should().Be(AutomaticRifle().MagazineCapacity - 1);
        machine.Phase.Should().Be(WeaponPhase.Cooldown);
    }

    [Fact]
    public void TryFire_EmptyMagazine_ReturnsEmpty()
    {
        var definition = new WeaponCoreDefinition(
            "empty",
            WeaponFireMode.SemiAutomatic,
            baseDamage: 10,
            fireIntervalTicks: 2,
            magazineCapacity: 1,
            startingReserveAmmo: 0,
            reloadDurationTicks: 5);
        var machine = new WeaponStateMachine(definition);
        _ = machine.TryFire(1, true, false, false, 1);
        machine.Advance(3);
        var empty = machine.TryFire(3, true, false, false, 1);
        empty.Code.Should().Be(WeaponActionCode.EmptyMagazine);
    }

    [Fact]
    public void Reload_CompletesAndRefillsMagazine()
    {
        var definition = new WeaponCoreDefinition(
            "reload",
            WeaponFireMode.SemiAutomatic,
            baseDamage: 10,
            fireIntervalTicks: 2,
            magazineCapacity: 2,
            startingReserveAmmo: 10,
            reloadDurationTicks: 5);
        var machine = new WeaponStateMachine(definition);
        _ = machine.TryFire(1, true, false, false, 1);
        machine.Advance(3);
        var reload = machine.TryStartReload(3);
        reload.Code.Should().Be(WeaponActionCode.ReloadStarted);
        machine.Phase.Should().Be(WeaponPhase.Reloading);
        machine.Advance(8);
        machine.Phase.Should().Be(WeaponPhase.Ready);
        machine.MagazineAmmo.Should().Be(2);
    }

    [Fact]
    public void EvaluateDamage_AppliesHeadMultiplier()
    {
        var definition = AutomaticRifle();
        var torso = WeaponBallistics.EvaluateDamage(
            definition,
            distanceMillimeters: 1_000,
            WeaponHitRegion.Torso);
        var head = WeaponBallistics.EvaluateDamage(
            definition,
            distanceMillimeters: 1_000,
            WeaponHitRegion.Head);
        head.Should().BeGreaterThan(torso);
    }

    private static WeaponCoreDefinition AutomaticRifle()
        => new(
            "ar",
            WeaponFireMode.Automatic,
            baseDamage: 25,
            fireIntervalTicks: 3,
            magazineCapacity: 30,
            startingReserveAmmo: 90,
            reloadDurationTicks: 20,
            hipSpreadMilliDegrees: 1_500,
            adsSpreadMilliDegrees: 200);
}
