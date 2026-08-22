namespace Ashlar.Abstractions.Database;

/// <summary>
/// Factory for creating isolated database instances from a <see cref="DatabaseProvisionRequest"/>.
/// </summary>
public interface IDatabaseProvisioner
{
    /// <summary>
    /// Creates a new isolated database according to the request.
    /// </summary>
    Task<IIsolatedDatabase> CreateAsync(DatabaseProvisionRequest request, CancellationToken cancellationToken = default);
}
