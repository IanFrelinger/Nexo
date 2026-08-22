using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>Validates input/output seams between bricks in a composition graph.</summary>
internal static class CompositionSeamChecker
{
    /// <summary>Check.</summary>
    public static SeamCheckResult Check(CompositionSpec spec, IBrickRegistry brickRegistry)
    {
        var violations = new List<string>();

        foreach (var edge in spec.Edges)
        {
            if (!TryResolvePortType(edge.SourceNodeId, edge.SourcePort, spec, brickRegistry, isOutput: true, out var sourceType))
            {
                violations.Add($"Unknown source port {edge.SourceNodeId}.{edge.SourcePort}");
                continue;
            }

            if (!TryResolvePortType(edge.TargetNodeId, edge.TargetPort, spec, brickRegistry, isOutput: false, out var targetType))
            {
                violations.Add($"Unknown target port {edge.TargetNodeId}.{edge.TargetPort}");
                continue;
            }

            if (!TypesCompatible(sourceType, targetType))
            {
                violations.Add(
                    $"Seam mismatch on edge {edge.SourceNodeId}.{edge.SourcePort} -> {edge.TargetNodeId}.{edge.TargetPort}: " +
                    $"producer type '{sourceType}' does not satisfy consumer type '{targetType}'");
            }
        }

        return new SeamCheckResult(violations.Count == 0, violations);
    }

    private static bool TryResolvePortType(
        string nodeId,
        string portName,
        CompositionSpec spec,
        IBrickRegistry brickRegistry,
        bool isOutput,
        out string type)
    {
        type = string.Empty;

        if (string.Equals(nodeId, CompositionGraphConstants.InputNodeId, StringComparison.Ordinal))
        {
            var port = spec.Inputs.FirstOrDefault(p => string.Equals(p.Name, portName, StringComparison.Ordinal));
            if (port is null)
                return false;
            type = port.Type;
            return true;
        }

        if (string.Equals(nodeId, CompositionGraphConstants.OutputNodeId, StringComparison.Ordinal))
        {
            if (!isOutput)
            {
                var port = spec.Outputs.FirstOrDefault(p => string.Equals(p.Name, portName, StringComparison.Ordinal));
                if (port is null)
                    return false;
                type = port.Type;
                return true;
            }

            return false;
        }

        var node = spec.Nodes.FirstOrDefault(n => string.Equals(n.NodeId, nodeId, StringComparison.Ordinal));
        if (node is null)
            return false;

        var brick = brickRegistry.GetBrick(node.BrickId);
        if (brick is null)
            return false;

        if (isOutput)
        {
            var output = brick.Interface.Outputs.FirstOrDefault(o => string.Equals(o.Name, portName, StringComparison.Ordinal));
            if (output is null)
                return false;
            type = output.Type;
            return true;
        }

        var input = brick.Interface.Inputs.FirstOrDefault(i => string.Equals(i.Name, portName, StringComparison.Ordinal));
        if (input is null)
            return false;
        type = input.Type;
        return true;
    }

    private static bool TypesCompatible(string producerType, string consumerType) =>
        string.Equals(producerType, consumerType, StringComparison.OrdinalIgnoreCase)
        || (string.Equals(producerType, "int", StringComparison.OrdinalIgnoreCase)
            && string.Equals(consumerType, "long", StringComparison.OrdinalIgnoreCase))
        || (string.Equals(producerType, "long", StringComparison.OrdinalIgnoreCase)
            && string.Equals(consumerType, "int", StringComparison.OrdinalIgnoreCase));

    internal sealed record SeamCheckResult(bool Passed, IReadOnlyList<string> Violations);
}
