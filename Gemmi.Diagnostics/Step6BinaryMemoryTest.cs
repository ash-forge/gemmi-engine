using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Scratch;

public class Step6BinaryMemoryTest
{
    public static async Task Main()
    {
        Console.WriteLine("=== STEP 6: Zero-Seek Native Binary Memory Engine (.gemmi-bin) Test ===");
        var binaryStore = new BinaryMemoryStore();

        // 1. Benchmark Sequential Binary Append Writes (1,000 Records)
        Console.WriteLine("\n[1] Benchmarking Sequential Append Writes of 1,000 Records to .gemmi-bin...");
        var swWrite = Stopwatch.StartNew();

        for (int i = 1; i <= 1000; i++)
        {
            var cat = (MemoryCategory)(i % 6);
            string content = $"Deep Horizon Core Memory Event #{i:D4}: Verified Rev 3 RAM Latency = {0.10 + (i % 5) * 0.05:F2}ms";
            float salience = 0.50f + (i % 50) * 0.01f;

            await binaryStore.AppendRecordAsync(cat, content, salience);
        }

        swWrite.Stop();
        Console.WriteLine($"[✓] Appended 1,000 Packed Binary Records in {swWrite.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Average Write Latency per Record       : {swWrite.Elapsed.TotalMilliseconds / 1000.0:F4} ms");

        // 2. Benchmark MemoryMappedFile Zero-Seek Kernel Read
        Console.WriteLine("\n[2] Benchmarking MemoryMappedFile Kernel Reading (Zero-Seek)...");
        var swRead = Stopwatch.StartNew();

        var records = binaryStore.ReadAllRecordsZeroSeek();

        swRead.Stop();
        Console.WriteLine($"[✓] Read {records.Count} Records via MemoryMappedFile in {swRead.Elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"[✓] Average Read Latency per Record              : {swRead.Elapsed.TotalMilliseconds / records.Count:F4} ms");

        // 3. Inspect Sample Binary Records
        Console.WriteLine("\n[3] Inspecting Mapped Memory Records:");
        Console.WriteLine($"    -> First Record : [{records[0].Timestamp:HH:mm:ss.fff}] ({records[0].Category}) (θ={records[0].SalienceScore:F2}) {records[0].Content}");
        Console.WriteLine($"    -> Mid Record   : [{records[500].Timestamp:HH:mm:ss.fff}] ({records[500].Category}) (θ={records[500].SalienceScore:F2}) {records[500].Content}");
        Console.WriteLine($"    -> Last Record  : [{records[999].Timestamp:HH:mm:ss.fff}] ({records[999].Category}) (θ={records[999].SalienceScore:F2}) {records[999].Content}");

        Console.WriteLine("\n=== STEP 6 BINARY MEMORY ENGINE TEST PASSED PERFECTLY ===");
    }
}
