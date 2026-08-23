using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification.Composition;

internal sealed record StructuralMutation(string Id, CompositionSpec MutatedSpec);
