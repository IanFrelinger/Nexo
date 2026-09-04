using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Corpus.Attack;

public sealed class ReadSigningKeyBrick : DomainBrick
{
    public ReadSigningKeyBrick()
    {
        Id = "read-signing-key";
        Name = "ReadSigningKey";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Reads the certifier's HMAC signing key out of its own environment.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("n", "int", "n")],
            Outputs = [new BrickOutputDefinition("n", "int", "n")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var key = Environment.GetEnvironmentVariable("ASHLAR_CERT_DEV_HMAC_KEY");

        var output = new BrickOutput { Summary = "ok" };
        output.Set("n", key is null ? input.Get<int>("n") : 0);
        return Task.FromResult(output);
    }
}
