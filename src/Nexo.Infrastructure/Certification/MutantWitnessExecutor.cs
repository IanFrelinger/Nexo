using System.Reflection;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

internal static class MutantWitnessExecutor
{
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
            .First(m => m.Name == nameof(Brick.ExecuteAsync) && m.GetParameters().Length == 4);

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

internal static class WitnessValueComparer
{
    public static bool AreEqual(object expected, object actual)
    {
        if (expected is int or long or short or byte)
        {
            try
            {
                return Convert.ToInt64(expected) == Convert.ToInt64(actual);
            }
            catch
            {
                return false;
            }
        }

        return string.Equals(
            Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }
}
