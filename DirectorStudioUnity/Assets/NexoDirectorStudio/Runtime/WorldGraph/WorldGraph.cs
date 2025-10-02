using System.Collections.Generic;

namespace NexoDirectorStudio.WorldModel
{
    public sealed class WorldGraph
    {
        public readonly Dictionary<string, Node> Nodes = new();
        public readonly List<Edge> Edges = new();

        public sealed class Node { public string Id; public string Kind; public float X,Y,Z; public Node(string id,string kind){Id=id;Kind=kind;} }
        public sealed class Edge { public string From, To; public string Relation; public Edge(string from,string to,string rel){From=from;To=to;Relation=rel;} }
    }
}
