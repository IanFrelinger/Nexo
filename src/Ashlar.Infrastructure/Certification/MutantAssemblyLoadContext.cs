using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Testing.CodeAnalysis;

namespace Ashlar.Infrastructure.Certification;

internal sealed class MutantAssemblyLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Creates a collectible assembly load context for mutant certification runs.
    /// </summary>
    /// <param name="name">
    /// Diagnostic name, normally the mutant assembly name. Unnamed contexts show up as
    /// "name=&lt;null&gt;" in <see cref="AssemblyLoadContext.All"/>, which makes a leaked or
    /// stuck context impossible to attribute to a caller.
    /// </param>
    public MutantAssemblyLoadContext(string name) : base(isCollectible: true, name: name)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
