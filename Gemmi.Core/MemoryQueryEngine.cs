using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gemmi.Core;

public class MemoryQueryResult
{
    public TimeSpan QueryLatency { get; set; }
    public List<MemoryEntry> RelevantEntries { get; set; } = new();
    public List<GraphNode> AssociatedConcepts { get; set; } = new();
    public float HighestSalience { get; set; } = 0.0f;
}

public class MemoryQueryEngine
{
    private readonly WorkingMemoryBuffer _buffer;
    private readonly EpisodicMemoryGraph _graph;

    public MemoryQueryEngine(WorkingMemoryBuffer buffer, EpisodicMemoryGraph graph)
    {
        _buffer = buffer;
        _graph = graph;
    }

    public MemoryQueryResult QuerySubFiveMs(string queryContext, MemoryCategory? category = null, float salienceThreshold = 0.85f)
    {
        var sw = Stopwatch.StartNew();
        var result = new MemoryQueryResult();

        // 1. Scan Working Memory Buffer (In-RAM)
        var pool = category.HasValue ? _buffer.GetByCategory(category.Value) : _buffer.GetRecent(50);

        result.RelevantEntries = pool
            .Where(e => string.IsNullOrEmpty(queryContext) || e.Content.Contains(queryContext, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.SalienceScore)
            .Take(10)
            .ToList();

        if (result.RelevantEntries.Count > 0)
        {
            result.HighestSalience = result.RelevantEntries.Max(e => e.SalienceScore);
        }

        // 2. Traversal In-Memory Episodic Graph
        if (!string.IsNullOrEmpty(queryContext))
        {
            result.AssociatedConcepts = _graph.GetRelatedConcepts(queryContext);
        }

        sw.Stop();
        result.QueryLatency = sw.Elapsed;
        return result;
    }
}
