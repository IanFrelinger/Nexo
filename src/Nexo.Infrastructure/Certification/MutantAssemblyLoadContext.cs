using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Infrastructure.Certification;

internal sealed class MutantAssemblyLoadContext : AssemblyLoadContext
{
    /// <summary>Creates a collectible assembly load context for mutant certification runs.</summary>
    public MutantAssemblyLoadContext() : base(isCollectible: true)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
