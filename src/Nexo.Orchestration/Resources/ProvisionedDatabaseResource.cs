using Nexo.Abstractions.Database;
using Nexo.Abstractions.Resources;

namespace Nexo.Orchestration.Resources;

/// <summary>
/// Wraps an <see cref="IIsolatedDatabase"/> as a <see cref="IProvisionedResource"/> for unified teardown.
/// </summary>
public sealed class ProvisionedDatabaseResource : IProvisionedResource
{
    private readonly IIsolatedDatabase _database;

    public ProvisionedDatabaseResource(IIsolatedDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public ProvisionedResourceKind Kind => ProvisionedResourceKind.IsolatedDatabase;

    public ValueTask DisposeAsync() => _database.DisposeAsync();
}
