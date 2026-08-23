using System.Xml.Linq;

namespace Ashlar.Infrastructure.Certification;

internal sealed record DependencyCheckResult(bool Passed, IReadOnlyList<string> Violations);
