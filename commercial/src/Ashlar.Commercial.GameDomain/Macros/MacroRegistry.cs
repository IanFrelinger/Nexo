using System.Collections.Concurrent;

namespace Ashlar.Commercial.GameDomain.Macros;
/// <summary>
/// Thread-safe, in-memory registry for <see cref="MacroDefinition"/> instances.
/// <para>
/// The registry serves as the single source of truth for all macros available in the
/// current process. It supports hot-registration and removal, making it safe for use in
/// long-running designer sessions where macros are added, updated, and shared in real time.
/// </para>
/// </summary>
public class MacroRegistry
{
    private readonly ConcurrentDictionary<string, MacroDefinition> _macros = new();

    /// <summary>Registers or updates a macro definition.</summary>
    /// <param name="macro">The macro to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="macro"/> is <c>null</c>.</exception>
    public void Register(MacroDefinition macro)
    {
        ArgumentNullException.ThrowIfNull(macro);
        _macros[macro.MacroId] = macro;
    }

    /// <summary>
    /// Retrieves a macro by its identifier.
    /// </summary>
    /// <param name="macroId">The macro identifier to look up.</param>
    /// <returns>The matching <see cref="MacroDefinition"/>, or <c>null</c> if not found.</returns>
    public MacroDefinition? Get(string macroId)
    {
        _macros.TryGetValue(macroId, out var macro);
        return macro;
    }

    /// <summary>Returns a snapshot of all currently registered macros.</summary>
    public IReadOnlyList<MacroDefinition> List()
    {
        return _macros.Values.ToList();
    }

    /// <summary>
    /// Removes a macro from the registry.
    /// </summary>
    /// <param name="macroId">The macro identifier to remove.</param>
    /// <returns><c>true</c> if the macro was found and removed; otherwise <c>false</c>.</returns>
    public bool Remove(string macroId)
    {
        return _macros.TryRemove(macroId, out _);
    }

    /// <summary>
    /// Exports a copy of a macro definition for external serialisation or sharing.
    /// </summary>
    /// <param name="macroId">The macro identifier to export.</param>
    /// <returns>A copy of the <see cref="MacroDefinition"/>, or <c>null</c> if not found.</returns>
    public MacroDefinition? Export(string macroId)
    {
        return Get(macroId);
    }

    /// <summary>
    /// Imports an externally-supplied macro definition into the registry.
    /// Equivalent to <see cref="Register"/> but named for semantic clarity at call sites
    /// that deal with cross-session sharing.
    /// </summary>
    /// <param name="macro">The macro definition to import.</param>
    public void Import(MacroDefinition macro)
    {
        Register(macro);
    }
}
