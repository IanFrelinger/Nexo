namespace Nexo.Certification.State;

/// <summary>
/// Wire-format snapshot of live mutable store head hash bundled alongside attested logs.
/// </summary>
public sealed class LiveStateSnapshot
{
    public LiveStateSnapshot(string currentStateHash)
    {
        if (string.IsNullOrWhiteSpace(currentStateHash))
            throw new ArgumentException("Current state hash is required.", nameof(currentStateHash));

        CurrentStateHash = currentStateHash;
    }

    public string CurrentStateHash { get; }
}
