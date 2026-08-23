namespace Ashlar.Abstractions.Database;

/// <summary>
/// Extensibility hook invoked after an <see cref="IIsolatedDatabase"/> is provisioned.
/// Register multiple implementations in DI; they run in registration order.
/// </summary>
public interface IDatabaseProvisioningHook
{
    /// <summary>
    /// Called after the inner provisioner returns a live database handle.
    /// </summary>
    Task AfterProvisionAsync(
        IIsolatedDatabase database,
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken = default);
}
