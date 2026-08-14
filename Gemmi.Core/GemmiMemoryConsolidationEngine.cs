using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Gemmi.Core;

public class ConsolidationResult
{
    public Guid MemoryId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MemoryCategory Category { get; set; }
    public float SalienceScore { get; set; }
    public List<string> ExtractedConcepts { get; set; } = new();
    public DateTime ConsolidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Biological Selective Long-Term Memory Consolidation Engine.
/// Bridges in-RAM short-term WorkingMemoryBuffer with zero-seek .gemmi-bin storage
/// and relational EpisodicMemoryGraph based on salience threshold (theta >= 0.85).
/// </summary>
public class GemmiMemoryConsolidationEngine : IDisposable
{
    private readonly WorkingMemoryBuffer _workingMemory;
    private readonly EpisodicMemoryGraph _episodicGraph;
    private readonly BinaryMemoryStore _binaryStore;
    private readonly float _salienceThreshold;

    private readonly ConcurrentQueue<MemoryEntry> _pendingConsolidationQueue = new();
    private readonly List<ConsolidationResult> _consolidationHistory = new();
    private readonly object _historyLock = new();

    private CancellationTokenSource? _workerCts;
    private Task? _consolidationTask;
    private bool _isRunning;

    private int _consolidatedCount;
    private int _transientPurgedCount;

    public event Action<ConsolidationResult>? OnMemoryConsolidated;
    public event Action<MemoryEntry>? OnTransientObservationIgnored;

    public float SalienceThreshold => _salienceThreshold;
    public int ConsolidatedCount => _consolidatedCount;
    public int TransientPurgedCount => _transientPurgedCount;
    public bool IsRunning => _isRunning;

    public IReadOnlyList<ConsolidationResult> History
    {
        get
        {
            lock (_historyLock)
            {
                return _consolidationHistory.ToArray();
            }
        }
    }

    public GemmiMemoryConsolidationEngine(
        WorkingMemoryBuffer workingMemory,
        EpisodicMemoryGraph episodicGraph,
        BinaryMemoryStore binaryStore,
        float salienceThreshold = 0.85f)
    {
        _workingMemory = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
        _episodicGraph = episodicGraph ?? throw new ArgumentNullException(nameof(episodicGraph));
        _binaryStore = binaryStore ?? throw new ArgumentNullException(nameof(binaryStore));
        _salienceThreshold = salienceThreshold;
    }

    public void Start()
    {
        if (_isRunning) return;

        _workerCts = new CancellationTokenSource();
        _workingMemory.OnObservationAdded += HandleNewObservation;
        _isRunning = true;

        _consolidationTask = Task.Run(() => ConsolidationWorkerLoopAsync(_workerCts.Token));
        Console.WriteLine($"[GemmiConsolidationEngine] Selective Long-Term Consolidation Active (θ >= {_salienceThreshold:F2}).");
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _workingMemory.OnObservationAdded -= HandleNewObservation;
        _workerCts?.Cancel();

        if (_consolidationTask != null)
        {
            try { await _consolidationTask; } catch { }
        }

        _isRunning = false;
        Console.WriteLine("[GemmiConsolidationEngine] Consolidation engine stopped.");
    }

    private void HandleNewObservation(MemoryEntry entry)
    {
        if (entry.SalienceScore >= _salienceThreshold)
        {
            _pendingConsolidationQueue.Enqueue(entry);
        }
        else
        {
            Interlocked.Increment(ref _transientPurgedCount);
            OnTransientObservationIgnored?.Invoke(entry);
        }
    }

    private async Task ConsolidationWorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_pendingConsolidationQueue.TryDequeue(out var entry))
                {
                    await ConsolidateEntryAsync(entry);
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GemmiConsolidationEngine] Error during consolidation: {ex.Message}");
                await Task.Delay(500, cancellationToken);
            }
        }
    }

    public async Task<ConsolidationResult> ConsolidateEntryAsync(MemoryEntry entry)
    {
        // 1. Commit to zero-seek .gemmi-bin binary disk persistence
        await _binaryStore.AppendRecordAsync(entry.Category, entry.Content, entry.SalienceScore);

        // 2. Extract key concepts/entities for Episodic Graph
        var concepts = ExtractKeyConcepts(entry.Content);

        (float X, float Y, float Z)? spatialPos = null;
        if (entry.Metadata.TryGetValue("spatialX", out var sx) &&
            entry.Metadata.TryGetValue("spatialY", out var sy) &&
            entry.Metadata.TryGetValue("spatialZ", out var sz) &&
            float.TryParse(sx, out var x) && float.TryParse(sy, out var y) && float.TryParse(sz, out var z))
        {
            spatialPos = (x, y, z);
        }

        (double Lat, double Lng, float Alt)? geoPos = null;
        if (entry.Metadata.TryGetValue("gpsLat", out var slat) &&
            entry.Metadata.TryGetValue("gpsLng", out var slng) &&
            double.TryParse(slat, out var lat) && double.TryParse(slng, out var lng))
        {
            float alt = 0.0f;
            if (entry.Metadata.TryGetValue("gpsAlt", out var salt) && float.TryParse(salt, out var parsedAlt)) alt = parsedAlt;
            geoPos = (lat, lng, alt);
        }

        // 3. Update Episodic Graph nodes and link co-occurring concepts
        GraphNode? primaryNode = null;
        if (concepts.Count > 0)
        {
            primaryNode = _episodicGraph.AddOrUpdateConcept(
                concepts[0],
                entry.Category,
                entry.SalienceScore,
                spatialPos,
                geoPos);

            for (int i = 1; i < concepts.Count; i++)
            {
                var secondaryNode = _episodicGraph.AddOrUpdateConcept(concepts[i], entry.Category, entry.SalienceScore * 0.8f);
                _episodicGraph.LinkConcepts(concepts[0], concepts[i], entry.Category, entry.Category);
            }
        }
        else
        {
            // If no individual words isolated, use full snippet
            string fallbackConcept = entry.Content.Length > 30 ? entry.Content.Substring(0, 30) + "..." : entry.Content;
            primaryNode = _episodicGraph.AddOrUpdateConcept(fallbackConcept, entry.Category, entry.SalienceScore, spatialPos, geoPos);
        }

        Interlocked.Increment(ref _consolidatedCount);

        var result = new ConsolidationResult
        {
            MemoryId = entry.Id,
            Content = entry.Content,
            Category = entry.Category,
            SalienceScore = entry.SalienceScore,
            ExtractedConcepts = concepts,
            ConsolidatedAt = DateTime.UtcNow
        };

        lock (_historyLock)
        {
            _consolidationHistory.Add(result);
            if (_consolidationHistory.Count > 200) _consolidationHistory.RemoveAt(0);
        }

        OnMemoryConsolidated?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Associative Memory Weaving (Offline "Dreaming").
    /// Clusters high-salience concepts and strengthens relational links.
    /// </summary>
    public int WeaveAssociativeMemories()
    {
        var highSalienceNodes = _episodicGraph.FindHighWeightConcepts(_salienceThreshold).ToList();
        int newLinksFormed = 0;

        for (int i = 0; i < highSalienceNodes.Count; i++)
        {
            for (int j = i + 1; j < highSalienceNodes.Count; j++)
            {
                var nodeA = highSalienceNodes[i];
                var nodeB = highSalienceNodes[j];

                // Link if same category or shared location
                if (nodeA.Category == nodeB.Category ||
                    (nodeA.HasSpatialLocation && nodeB.HasSpatialLocation))
                {
                    if (!nodeA.RelatedNodeIds.Contains(nodeB.Id))
                    {
                        _episodicGraph.LinkConcepts(nodeA.Concept, nodeB.Concept, nodeA.Category, nodeB.Category);
                        newLinksFormed++;
                    }
                }
            }
        }

        return newLinksFormed;
    }

    private static List<string> ExtractKeyConcepts(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        // Remove common stopwords and isolate significant keywords/entities
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for", "with",
            "is", "was", "are", "were", "it", "this", "that", "i", "you", "we", "they",
            "of", "by", "from", "as", "be", "have", "has", "had", "do", "does", "did"
        };

        var words = Regex.Matches(text, @"\b[A-Za-z0-9_#-]{3,}\b")
            .Select(m => m.Value)
            .Where(w => !stopwords.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return words;
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}
