using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Scoping;

namespace Nexo.Commercial.GameDomain.Contracts;

/// <summary>
/// Request to execute a registered macro.
/// </summary>
/// <param name="MacroId">Identifier of the macro to run.</param>
/// <param name="ParameterOverrides">Optional parameter overrides for the macro invocation.</param>
public sealed record ForgeMacroRunRequest(
    string MacroId,
    IReadOnlyDictionary<string, object>? ParameterOverrides);
