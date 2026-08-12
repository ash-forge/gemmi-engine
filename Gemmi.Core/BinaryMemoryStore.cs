using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gemmi.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedMemoryRecordHeader
{
    public long TimestampTicks;   // 8 bytes: UtcNow.Ticks
    public byte CategoryByte;     // 1 byte : MemoryCategory enum
    public float SalienceScore;   // 4 bytes: 0.00f to 1.00f
    public long StringOffset;     // 8 bytes: Payload string offset in string blob file
    public int StringLength;      // 4 bytes: Length of payload in bytes
    public ushort Checksum;       // 2 bytes: CRC16 Data Integrity
    public byte Reserved1;        // 1 byte
    public byte Reserved2;        // 1 byte
    public byte Reserved3;        // 1 byte
    public byte Reserved4;        // 1 byte
    public byte Reserved5;        // 1 byte (Total = 32 Bytes Header!)
}

public class BinaryMemoryRecord
{
    public DateTime Timestamp { get; set; }
    public MemoryCategory Category { get; set; }
    public float SalienceScore { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class BinaryMemoryStore
{
    private readonly string _indexPath;
    private readonly string _blobPath;
    private static readonly object _syncLock = new();
    private const int HeaderSize = 32;

    public BinaryMemoryStore(string baseDirectory = @"C:\Users\admin\.gemmi")
    {
        Directory.CreateDirectory(baseDirectory);
        _indexPath = Path.Combine(baseDirectory, "gemmi_memory_index.gemmi-bin");
        _blobPath = Path.Combine(baseDirectory, "gemmi_memory_payloads.gemmi-dat");
    }

    public async Task AppendRecordAsync(MemoryCategory category, string content, float salienceScore)
    {
        await Task.Run(() =>
        {
            lock (_syncLock)
            {
                byte[] stringBytes = Encoding.UTF8.GetBytes(content);

                // 1. Append payload to string blob file (Sequential Write)
                long stringOffset = 0;
                using (var blobStream = new FileStream(_blobPath, FileMode.Append, FileAccess.Write, FileShare.Read, 65536))
                {
                    stringOffset = blobStream.Position;
                    blobStream.Write(stringBytes, 0, stringBytes.Length);
                }

                // 2. Prepare 32-byte Packed Header
                var header = new PackedMemoryRecordHeader
                {
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    CategoryByte = (byte)category,
                    SalienceScore = salienceScore,
                    StringOffset = stringOffset,
                    StringLength = stringBytes.Length,
                    Checksum = ComputeCrc16(stringBytes)
                };

                byte[] headerBytes = StructureToByteArray(header);

                // 3. Append header to index file (Sequential Write - Zero Seek!)
                using (var indexStream = new FileStream(_indexPath, FileMode.Append, FileAccess.Write, FileShare.Read, 65536))
                {
                    indexStream.Write(headerBytes, 0, headerBytes.Length);
                }
            }
        });
    }

    public List<BinaryMemoryRecord> ReadAllRecordsZeroSeek()
    {
        var records = new List<BinaryMemoryRecord>();
        lock (_syncLock)
        {
            if (!File.Exists(_indexPath) || !File.Exists(_blobPath)) return records;

            var indexFileInfo = new FileInfo(_indexPath);
            long totalHeaders = indexFileInfo.Length / HeaderSize;
            if (totalHeaders == 0) return records;

            using var indexMmf = MemoryMappedFile.CreateFromFile(_indexPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var indexAccessor = indexMmf.CreateViewAccessor(0, indexFileInfo.Length, MemoryMappedFileAccess.Read);

            using var blobMmf = MemoryMappedFile.CreateFromFile(_blobPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var blobAccessor = blobMmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            for (long i = 0; i < totalHeaders; i++)
            {
                long position = i * HeaderSize;
                indexAccessor.Read(position, out PackedMemoryRecordHeader header);

                byte[] stringBytes = new byte[header.StringLength];
                blobAccessor.ReadArray(header.StringOffset, stringBytes, 0, header.StringLength);

                string content = Encoding.UTF8.GetString(stringBytes);

                records.Add(new BinaryMemoryRecord
                {
                    Timestamp = new DateTime(header.TimestampTicks, DateTimeKind.Utc),
                    Category = (MemoryCategory)header.CategoryByte,
                    SalienceScore = header.SalienceScore,
                    Content = content
                });
            }
        }
        return records;
    }

    private static ushort ComputeCrc16(byte[] data)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc = (ushort)(crc >> 1);
            }
        }
        return crc;
    }

    private static byte[] StructureToByteArray<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
}
