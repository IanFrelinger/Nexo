using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

internal sealed record StructuralMutation(string Id, CompositionSpec MutatedSpec);
