using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

internal static class CompositionSpecJsonParser
{
    public static CompositionSpec Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<CompositionSpecDto>(
            ExtractJson(json),
            JsonOptions) ?? throw new InvalidOperationException("Model returned empty composition JSON.");

        return FromDto(dto);
    }

    public static CompositionSpec FromDto(CompositionSpecDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompositionId))
            throw new InvalidOperationException("Model composition JSON missing compositionId.");

        return new CompositionSpec(
            dto.CompositionId,
            dto.Nodes?.Select(n => new CompositionNode(n.NodeId, n.BrickId)).ToList()
                ?? throw new InvalidOperationException("Model composition JSON missing nodes."),
            dto.Edges?.Select(e => new CompositionEdge(e.SourceNodeId, e.SourcePort, e.TargetNodeId, e.TargetPort)).ToList()
                ?? throw new InvalidOperationException("Model composition JSON missing edges."),
            dto.Inputs?.Select(p => new CompositionPort(p.Name, p.Type)).ToList()
                ?? throw new InvalidOperationException("Model composition JSON missing inputs."),
            dto.Outputs?.Select(p => new CompositionPort(p.Name, p.Type)).ToList()
                ?? throw new InvalidOperationException("Model composition JSON missing outputs."));
    }

    public static CompositionSpecDto ToDto(CompositionSpec spec) =>
        new()
        {
            CompositionId = spec.CompositionId,
            Nodes = spec.Nodes.Select(n => new NodeDto { NodeId = n.NodeId, BrickId = n.BrickId }).ToList(),
            Edges = spec.Edges.Select(e => new EdgeDto
            {
                SourceNodeId = e.SourceNodeId,
                SourcePort = e.SourcePort,
                TargetNodeId = e.TargetNodeId,
                TargetPort = e.TargetPort
            }).ToList(),
            Inputs = spec.Inputs.Select(p => new PortDto { Name = p.Name, Type = p.Type }).ToList(),
            Outputs = spec.Outputs.Select(p => new PortDto { Name = p.Name, Type = p.Type }).ToList()
        };

    public static string Serialize(CompositionSpec spec) =>
        JsonSerializer.Serialize(ToDto(spec), JsonOptions);

    private static string ExtractJson(string response)
    {
        const string fence = "```";
        var start = response.IndexOf(fence, StringComparison.Ordinal);
        if (start >= 0)
        {
            var afterFence = response.IndexOf('\n', start);
            var end = response.IndexOf(fence, afterFence + 1, StringComparison.Ordinal);
            if (afterFence >= 0 && end > afterFence)
                return response[(afterFence + 1)..end].Trim();
        }

        return response.Trim();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal sealed class CompositionSpecDto
    {
        public string CompositionId { get; set; } = "";
        public List<NodeDto>? Nodes { get; set; }
        public List<EdgeDto>? Edges { get; set; }
        public List<PortDto>? Inputs { get; set; }
        public List<PortDto>? Outputs { get; set; }
    }

    internal sealed class NodeDto
    {
        public string NodeId { get; set; } = "";
        public string BrickId { get; set; } = "";
    }

    internal sealed class EdgeDto
    {
        public string SourceNodeId { get; set; } = "";
        public string SourcePort { get; set; } = "";
        public string TargetNodeId { get; set; } = "";
        public string TargetPort { get; set; } = "";
    }

    internal sealed class PortDto
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
