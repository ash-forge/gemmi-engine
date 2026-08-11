using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Gemmi.Core;

public class ModelDownloaderService
{
    public string ModelsDirectory { get; }

    public ModelDownloaderService()
    {
        ModelsDirectory = Path.Combine(AppContext.BaseDirectory, "Models");
        if (!Directory.Exists(ModelsDirectory))
        {
            Directory.CreateDirectory(ModelsDirectory);
        }
    }

    public async Task EnsureModelsInstalledAsync(GemmiState state)
    {
        var t5GemmaDir = Path.Combine(ModelsDirectory, "t5gemma-tts");
        var paliGemmaDir = Path.Combine(ModelsDirectory, "paligemma2-3b");
        var gemmaTurboFallback = @"C:\Users\admin\gemma4-turbo-family";

        bool hasVoice = Directory.Exists(t5GemmaDir) || Directory.Exists(Path.Combine(gemmaTurboFallback, "t5gemma-tts-2b-2b"));
        bool hasVision = Directory.Exists(paliGemmaDir) || Directory.Exists(Path.Combine(gemmaTurboFallback, "paligemma2-3b"));

        if (hasVoice && hasVision)
        {
            state.WorkingMemoryGraph["ModelDownloader"] = "All required models (T5Gemma-TTS, PaliGemma 2) are installed and ready.";
            return;
        }

        state.WorkingMemoryGraph["ModelDownloader"] = "Missing models detected. Auto-connecting to Hugging Face to download required weights into ./Models...";
        Console.WriteLine($"[ModelDownloader]: Missing models detected. Auto-fetching into {ModelsDirectory}...");

        try
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "download_gemmaverse.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = @"C:\Users\admin\.gemini\antigravity-cli\brain\4892e880-a9ff-41fb-b7b7-5e990ca73e75\scratch\download_gemmaverse.py";
            }

            if (File.Exists(scriptPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\Users\admin\AppData\Local\Python\bin\python.exe",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        state.WorkingMemoryGraph["ModelDownloader"] = "Models successfully downloaded from Hugging Face into ./Models.";
                        Console.WriteLine("[ModelDownloader]: Hugging Face download finished successfully.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ModelDownloader Error]: {ex.Message}");
            state.WorkingMemoryGraph["ModelDownloader"] = $"Download warning: {ex.Message}";
        }
    }
}
