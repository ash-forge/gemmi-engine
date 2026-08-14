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

    // 3D Spatial Vector Coordinates relative to desk/origin (X, Y, Z meters)
    public (float X, float Y, float Z) SpatialVector { get; set; } = (0.0f, 0.0f, 0.0f);

    // Sub-meter GPS GeoCoordinates (Latitude, Longitude, Altitude)
    public (double Latitude, double Longitude, float Altitude) GeoCoordinates { get; set; } = (0.0, 0.0, 0.0f);

    public bool HasSpatialLocation => SpatialVector != (0.0f, 0.0f, 0.0f) || GeoCoordinates != (0.0, 0.0, 0.0f);
}

public class EpisodicMemoryGraph
{
    private readonly ConcurrentDictionary<Guid, GraphNode> _nodes = new();
    private readonly ConcurrentDictionary<string, Guid> _conceptIndex = new(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _nodes.Count;

    public GraphNode AddOrUpdateConcept(string concept, MemoryCategory category, float weight = 1.0f, (float X, float Y, float Z)? spatialVector = null, (double Lat, double Lng, float Alt)? geoCoordinates = null)
    {
        if (_conceptIndex.TryGetValue(concept, out var existingId) && _nodes.TryGetValue(existingId, out var existingNode))
        {
            existingNode.Weight += weight;
            if (spatialVector.HasValue) existingNode.SpatialVector = spatialVector.Value;
            if (geoCoordinates.HasValue) existingNode.GeoCoordinates = geoCoordinates.Value;
            return existingNode;
        }

        var node = new GraphNode
        {
            Concept = concept,
            Category = category,
            Weight = weight
        };

        if (spatialVector.HasValue) node.SpatialVector = spatialVector.Value;
        if (geoCoordinates.HasValue) node.GeoCoordinates = geoCoordinates.Value;

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

    // 3D Spatial Proximity Lookup using Euclidean Distance: sqrt(dx^2 + dy^2 + dz^2)
    public List<(GraphNode Node, float DistanceMeters)> GetConceptsBySpatialProximity(float x, float y, float z, float radiusMeters = 2.0f)
    {
        var matches = new List<(GraphNode Node, float DistanceMeters)>();

        foreach (var node in _nodes.Values)
        {
            if (!node.HasSpatialLocation) continue;

            float dx = node.SpatialVector.X - x;
            float dy = node.SpatialVector.Y - y;
            float dz = node.SpatialVector.Z - z;
            float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            if (distance <= radiusMeters)
            {
                matches.Add((node, distance));
            }
        }

        return matches.OrderBy(m => m.DistanceMeters).ToList();
    }

    public IEnumerable<GraphNode> FindHighWeightConcepts(float minWeight = 0.85f)
    {
        return _nodes.Values.Where(n => n.Weight >= minWeight);
    }

    public void Clear()
    {
        _nodes.Clear();
        _conceptIndex.Clear();
    }
}
