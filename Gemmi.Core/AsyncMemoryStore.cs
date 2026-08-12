using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gemmi.Core;

public class AsyncMemoryStore
{
    private readonly ConcurrentQueue<MemoryEntry> _flushQueue = new();
    private readonly string _storageFilePath;
    private bool _isFlushing;

    public AsyncMemoryStore(string? customPath = null)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Models");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _storageFilePath = customPath ?? Path.Combine(dir, "gemmi_episodic_memory.jsonl");
    }

    public void EnqueueForBackgroundFlush(MemoryEntry entry)
    {
        _flushQueue.Enqueue(entry);
    }

    public async Task StartBackgroundFlushLoopAsync(CancellationToken cancellationToken)
    {
        _isFlushing = true;
        while (_isFlushing && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(2000, cancellationToken); // Flush every 2 seconds asynchronously

            if (_flushQueue.IsEmpty) continue;

            try
            {
                using var writer = new StreamWriter(_storageFilePath, append: true);
                while (_flushQueue.TryDequeue(out var entry))
                {
                    var json = JsonSerializer.Serialize(entry);
                    await writer.WriteLineAsync(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsyncMemoryStore Error]: {ex.Message}");
            }
        }
    }

    public void Stop() => _isFlushing = false;
}
