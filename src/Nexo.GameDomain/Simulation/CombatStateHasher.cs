using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.GameDomain.Simulation;

/// <summary>Computes a canonical hash of authoritative combat rule state.</summary>
public static class CombatStateHasher
{
    /// <summary>
    /// Hashes the processed tick and sorted combatant state. Unity transforms and
    /// physics contacts are intentionally not included.
    /// </summary>
    public static string Compute(CombatSimulation simulation)
    {
        if (simulation is null)
            throw new ArgumentNullException(nameof(simulation));

        var canonical = new StringBuilder();
        canonical.Append("tick=")
            .Append(simulation.CurrentTick.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        foreach (var combatant in simulation.EnumerateCombatants())
        {
            canonical.Append(combatant.Id.Value.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(combatant.MaximumHealth.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(combatant.Health.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(combatant.Armor.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return hex.ToString();
    }
}
