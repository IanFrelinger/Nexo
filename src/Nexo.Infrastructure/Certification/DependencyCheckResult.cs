using System.Xml.Linq;

namespace Nexo.Infrastructure.Certification;

internal sealed record DependencyCheckResult(bool Passed, IReadOnlyList<string> Violations);
