using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Testing.CodeAnalysis;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>Compiled output from building a generated certification brick.</summary>
/// <param name="Brick">Loaded domain brick instance.</param>
/// <param name="BrickTypeName">Fully qualified generated brick type name.</param>
/// <param name="SourceCode">Generated brick source code.</param>
/// <param name="ProjectPath">Path to the generated brick project.</param>
/// <param name="CompilationReferences">Assembly references used during compilation.</param>
public sealed record BuiltGeneratedBrick(
    DomainBrick Brick,
    string BrickTypeName,
    string SourceCode,
    string ProjectPath,
    IReadOnlyList<string> CompilationReferences);
