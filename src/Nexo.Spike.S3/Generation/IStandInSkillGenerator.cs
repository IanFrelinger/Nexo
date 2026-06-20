using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

public interface IStandInSkillGenerator
{
    string BackendName { get; }

    int GenerationCount { get; }

    SkillCandidate Generate(IntentDescriptor intent);
}
