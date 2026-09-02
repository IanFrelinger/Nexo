using System.Reflection;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification;

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
        catch (CertificationHarnessException)
        {
            // The harness broke, not the mutant. Swallowing this is how a mutation leg reports
            // a clean sweep it never ran; it propagates so the gate produces no verdict at all.
            throw;
        }
        catch
        {
            // A mutant that throws while EXECUTING is a genuine kill: the witness drove it and
            // it could not produce the expected output.
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
        // Named refusal, not First()-throws: the difference between "the harness has no execution
        // context" and "the mutant threw" is the difference between a vacuous kill and a real one,
        // and an InvalidOperationException out of First() is indistinguishable from the latter.
        var auditContextType = Array.Find(
            mutantAssembly.GetTypes(),
            t => t.Name == CandidateSourceWrapper.AuditContextTypeName)
            ?? throw CertificationHarnessException.MissingAuditContext(mutantAssembly.GetName().Name ?? "<mutant>");
        var auditContext = auditContextType
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
