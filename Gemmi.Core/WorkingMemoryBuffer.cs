using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Gemmi.Core;

public enum MemoryCategory
{
    Vision,
    Voice,
    Location,
    Code,
    System,
    Thought
}

public class MemoryEntry
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public MemoryCategory Category { get; set; }
    public string Content { get; set; } = string.Empty;
    public float SalienceScore { get; set; } = 0.0f; // Score (theta)
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class WorkingMemoryBuffer
{
    private readonly ConcurrentQueue<MemoryEntry> _buffer = new();
    private readonly int _maxCapacity;

    public event Action<MemoryEntry>? OnObservationAdded;

    public int Count => _buffer.Count;

    public WorkingMemoryBuffer(int maxCapacity = 500)
    {
        _maxCapacity = maxCapacity;
    }

    public MemoryEntry AddObservation(MemoryCategory category, string content, float salienceScore = 0.0f, Dictionary<string, string>? metadata = null)
    {
        var entry = new MemoryEntry
        {
            Category = category,
            Content = content,
            SalienceScore = salienceScore,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        _buffer.Enqueue(entry);
        OnObservationAdded?.Invoke(entry);

        // Maintain fixed ring buffer size in RAM
        while (_buffer.Count > _maxCapacity && _buffer.TryDequeue(out _))
        {
            // Truncate oldest entries to prevent RAM bloat
        }

        return entry;
    }

    public List<MemoryEntry> GetRecent(int count = 20)
    {
        return _buffer.ToArray().TakeLast(count).ToList();
    }

    public List<MemoryEntry> GetHighSalience(float threshold = 0.85f)
    {
        return _buffer.Where(e => e.SalienceScore >= threshold).ToList();
    }

    public List<MemoryEntry> GetByCategory(MemoryCategory category, int count = 20)
    {
        return _buffer.Where(e => e.Category == category).TakeLast(count).ToList();
    }

    public void Clear()
    {
        _buffer.Clear();
    }
}
