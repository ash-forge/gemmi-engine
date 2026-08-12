using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Gemmi.Core;

public class GraphNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Concept { get; set; } = string.Empty;
    public MemoryCategory Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public float Weight { get; set; } = 1.0f;
    public HashSet<Guid> RelatedNodeIds { get; } = new();
}

public class EpisodicMemoryGraph
{
    private readonly ConcurrentDictionary<Guid, GraphNode> _nodes = new();
    private readonly ConcurrentDictionary<string, Guid> _conceptIndex = new(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _nodes.Count;

    public GraphNode AddOrUpdateConcept(string concept, MemoryCategory category, float weight = 1.0f)
    {
        if (_conceptIndex.TryGetValue(concept, out var existingId) && _nodes.TryGetValue(existingId, out var existingNode))
        {
            existingNode.Weight += weight;
            return existingNode;
        }

        var node = new GraphNode
        {
            Concept = concept,
            Category = category,
            Weight = weight
        };

        _nodes[node.Id] = node;
        _conceptIndex[concept] = node.Id;
        return node;
    }

    public void LinkConcepts(string conceptA, string conceptB, MemoryCategory catA = MemoryCategory.Thought, MemoryCategory catB = MemoryCategory.Thought)
    {
        var nodeA = AddOrUpdateConcept(conceptA, catA);
        var nodeB = AddOrUpdateConcept(conceptB, catB);

        lock (nodeA.RelatedNodeIds) lock (nodeB.RelatedNodeIds)
        {
            nodeA.RelatedNodeIds.Add(nodeB.Id);
            nodeB.RelatedNodeIds.Add(nodeA.Id);
        }
    }

    public List<GraphNode> GetRelatedConcepts(string concept, int depth = 1)
    {
        if (!_conceptIndex.TryGetValue(concept, out var rootId) || !_nodes.TryGetValue(rootId, out var rootNode))
        {
            return new List<GraphNode>();
        }

        var result = new List<GraphNode>();
        foreach (var relatedId in rootNode.RelatedNodeIds)
        {
            if (_nodes.TryGetValue(relatedId, out var relatedNode))
            {
                result.Add(relatedNode);
            }
        }

        return result.OrderByDescending(n => n.Weight).ToList();
    }

    public void Clear()
    {
        _nodes.Clear();
        _conceptIndex.Clear();
    }
}
