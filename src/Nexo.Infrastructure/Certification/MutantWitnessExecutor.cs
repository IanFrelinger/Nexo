using System.Reflection;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

/// <summary>Runs witness specs against compiled mutant brick instances during certification.</summary>
internal static class MutantWitnessExecutor
{
    /// <summary>Run witness asynchronously.</summary>
    public static async Task<bool> RunWitnessAsync(
        object mutantInstance,
        Assembly mutantAssembly,
        WitnessSpec witness,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunWitnessCoreAsync(
                mutantInstance,
                mutantAssembly,
                witness,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> RunWitnessCoreAsync(
        object mutantInstance,
        Assembly mutantAssembly,
        WitnessSpec witness,
        CancellationToken cancellationToken)
    {
        var executeMethod = mutantInstance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == nameof(DomainBrick.ExecuteAsync) && m.GetParameters().Length == 4);

        var parameters = executeMethod.GetParameters();
        var inputType = parameters[0].ParameterType;
        var implementationType = parameters[1].ParameterType;
        var auditContext = mutantAssembly
            .GetTypes()
            .First(t => t.Name == "CertAuditContext")
            .GetConstructor(Type.EmptyTypes)!
            .Invoke(null);

        foreach (var (caseIndex, witnessCase) in witness.Cases.Select((c, i) => (i, c)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mutantInput = Activator.CreateInstance(inputType, witnessCase.Input);
            var mutantImplementation = Enum.ToObject(implementationType, ImplementationType.Deterministic);
            var task = (Task)executeMethod.Invoke(
                mutantInstance,
                [mutantInput, mutantImplementation, auditContext, cancellationToken])!;

            await task.ConfigureAwait(false);
            var mutantOutput = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
            if (mutantOutput is null)
                return false;

            var outputData = (IReadOnlyDictionary<string, object>)mutantOutput
                .GetType()
                .GetMethod(nameof(BrickOutput.ToDictionary))!
                .Invoke(mutantOutput, null)!;
            // Same observable view the in-process witness judges: keyed data plus the summary
            // under the reserved key, so a mutated summary literal is killable by a witness.
            var mutantSummary = mutantOutput.GetType().GetProperty(nameof(BrickOutput.Summary))?.GetValue(mutantOutput) as string;
            outputData = WitnessObservableOutput.Project(outputData, mutantSummary);

            foreach (var (key, expected) in witnessCase.ExpectedOutput)
            {
                if (!outputData.TryGetValue(key, out var actual))
                    return false;

                if (!WitnessValueComparer.AreEqual(expected, actual))
                    return false;
            }
        }

        return true;
    }
}
