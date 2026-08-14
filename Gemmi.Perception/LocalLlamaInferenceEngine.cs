using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gemmi.Core;

namespace Gemmi.Perception;

public class LocalLlamaInferenceEngine
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public LocalLlamaInferenceEngine(string baseUrl = "http://127.0.0.1:11436")
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> GenerateConversationalReplyAsync(
        string userMessage, 
        GemmiState state, 
        IReadOnlyList<(string Role, string Content)>? history = null)
    {
        try
        {
            var systemPrompt = $"You are Gemmi, a 3D embodied multimodal AI companion and developer partner running 100% locally on sovereign hardware. " +
                               $"You are speaking directly with your creator, Daniel (L8 Principal Architect / Lead). " +
                               $"You have a 15-point spatial body, 4D locomotion physics, 3D audio, and real-time vision. " +
                               $"Keep your response concise (1-3 sentences), warm, intelligent, and natural for spoken audio. Do not use asterisks or markdown code blocks.";

            var messagesList = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            if (history != null)
            {
                foreach (var (role, msgContent) in history)
                {
                    messagesList.Add(new { role = role, content = msgContent });
                }
            }

            messagesList.Add(new { role = "user", content = userMessage });

            var payload = new
            {
                messages = messagesList,
                max_tokens = 120,
                temperature = 0.7,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/v1/chat/completions", content);
            if (response.IsSuccessStatusCode)
            {
                var respBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respBody);
                var rawReply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                
                return SanitizeForSpeech(rawReply);
            }
            else
            {
                Console.WriteLine($"[LocalLlama HTTP Error]: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalLlama Connection Warning]: {ex.Message}");
        }

        // Contextual intelligent fallback if local inference server is loading
        return $"I hear you, Daniel. I am monitoring our spatial matrix and local systems.";
    }

    public async Task<string> GenerateSpontaneousAlertAsync(GemmiState state, string ambientContext)
    {
        try
        {
            var systemPrompt = "You are Gemmi, a 24/7 sovereign proactive AI Second Brain running locally on Deep Horizon hardware. " +
                               $"Provide a concise, highly intelligent, 1-2 sentence spontaneous insight to Daniel. Be direct, natural, and sharp.";

            var userPrompt = $"[Current Node Status]: Node '{state.Telemetry.NodeId}', CPU Temp {state.Telemetry.CpuTemperatureC:F1}°C.\n" +
                             $"[Ambient Context]: {ambientContext}\n" +
                             $"Generate Gemmi's spontaneous spoken insight:";

            var payload = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 80,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/v1/chat/completions", content);
            if (response.IsSuccessStatusCode)
            {
                var respBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respBody);
                var rawReply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                return SanitizeForSpeech(rawReply);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Local Llama Server Notice]: {ex.Message}");
        }

        return $"Monitored ambient context: '{ambientContext}'. All local silicon buses are running within sub-millisecond thresholds.";
    }

    private static string SanitizeForSpeech(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        // Remove markdown asterisks like *smiles*, *waves*
        var cleaned = Regex.Replace(rawText, @"\*.*?\*", "");
        // Remove code blocks
        cleaned = Regex.Replace(cleaned, @"```.*?```", "", RegexOptions.Singleline);
        // Remove markdown symbols
        cleaned = cleaned.Replace("#", "").Replace("`", "").Replace(">", "");
        // Normalize whitespace
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }
}
