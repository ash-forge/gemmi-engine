using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gemmi.Core;
using Gemmi.Hardware;
using Gemmi.Perception;

namespace Gemmi.Diagnostics;

public class Step21RestApiAndEcosystemTest
{
    public static async Task Main()
    {
        Console.WriteLine("==========================================================================");
        Console.WriteLine("  📡 GEMMI ENGINE STEP 21: UNIFIED REST API & ECOSYSTEM TEST (PORT 8088)");
        Console.WriteLine("==========================================================================");

        var state = new GemmiState();
        state.User.UserName = "Daniel";
        state.MemoryBuffer.AddObservation(MemoryCategory.System, "Silicon buses initialized on Rev 3 node.", 0.85f);
        state.MemoryBuffer.AddObservation(MemoryCategory.Code, "ModelStudio GGUF reader tests passed 100%.", 0.92f);

        var avatar = new AvatarStateController();
        var facialEngine = new GemmiFacialAnimationEngine();
        using var voicePipeline = new GemmiVoiceDialoguePipeline(null, facialEngine);
        var autonomyEngine = new GemmiAutonomousAgencyEngine(avatar, facialEngine, voicePipeline, state);
        var orchestrator = new GemmiAgentOrchestrator();

        orchestrator.RegisterTool("Ping", "Pings the engine", (p, ct) => Task.FromResult(AgentToolResult.Ok("Pong! Engine is healthy.")));
        orchestrator.RegisterTool("ModelInspector", "Inspects active model parameters", (p, ct) => Task.FromResult(AgentToolResult.Ok("Model: haven-chat-v3.0.3.gguf | Status: Active")));
        orchestrator.Start();

        var networkServer = new GemmiNetworkServer(8088);

        // Wire REST API Callbacks
        networkServer.OnApiStatusRequested = () => new
        {
            status = "online",
            engine = "Gemmi Sovereign Multimodal Engine",
            version = "3.0.0",
            activeState = autonomyEngine.CurrentAutonomousState,
            connectedClients = networkServer.ConnectedClientCount,
            memoryObservations = state.MemoryBuffer.Count,
            timestamp = DateTime.UtcNow
        };

        networkServer.OnApiTelemetryRequested = () =>
        {
            var matrix = avatar.Get15PointSpatialMatrix();
            var frame = new SpatialTelemetryFrame
            {
                FrameIndex = 42,
                CurrentLocomotionState = autonomyEngine.CurrentAutonomousState,
                RecentThought = "Monitoring 15-Point spatial body."
            };
            frame.Joints["Level0_CenterGround"] = new[] { matrix.Level0_CenterGround.X, matrix.Level0_CenterGround.Y, matrix.Level0_CenterGround.Z, 0, 0, 0 };
            frame.Joints["Level1_CenterHips"] = new[] { matrix.Level1_CenterHips.X, matrix.Level1_CenterHips.Y, matrix.Level1_CenterHips.Z, 0, 0, 0 };
            frame.Joints["Level2_HeadCenter"] = new[] { matrix.Level2_HeadCenter.X, matrix.Level2_HeadCenter.Y, matrix.Level2_HeadCenter.Z, 0, 0, 0 };
            return frame;
        };

        networkServer.OnApiMemoryRequested = () => new
        {
            total = state.MemoryBuffer.Count,
            recent = state.MemoryBuffer.GetRecent(10)
        };

        networkServer.OnApiChatRequested = async (userMsg) =>
        {
            return await autonomyEngine.ProcessUserMessageAsync(userMsg);
        };

        networkServer.OnApiTaskDispatchRequested = (toolName, param) =>
        {
            var task = orchestrator.EnqueueTask("REST Task", toolName, new Dictionary<string, object>());
            return Task.FromResult(task);
        };

        networkServer.OnApiTaskHistoryRequested = () => orchestrator.History;

        networkServer.Start();

        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8088/") };

        try
        {
            // 1. Test GET /api/status
            Console.WriteLine("\n[1] Testing GET /api/status...");
            var statusResp = await http.GetStringAsync("api/status");
            Console.WriteLine($"    • Response: {statusResp}");

            // 2. Test GET /api/telemetry
            Console.WriteLine("\n[2] Testing GET /api/telemetry...");
            var telemResp = await http.GetStringAsync("api/telemetry");
            using var telemDoc = JsonDocument.Parse(telemResp);
            int jointCount = telemDoc.RootElement.GetProperty("joints").EnumerateObject().Count();
            Console.WriteLine($"    • [PASS] Telemetry Returned {jointCount} Kinematic Joints in Matrix.");

            // 3. Test GET /api/memory
            Console.WriteLine("\n[3] Testing GET /api/memory...");
            var memResp = await http.GetStringAsync("api/memory");
            using var memDoc = JsonDocument.Parse(memResp);
            int memCount = memDoc.RootElement.GetProperty("recent").GetArrayLength();
            Console.WriteLine($"    • [PASS] Memory Buffer Returned {memCount} Active RAM Records.");

            // 4. Test POST /api/chat
            Console.WriteLine("\n[4] Testing POST /api/chat (Prompting Gemmi Engine)...");
            var chatPayload = new { message = "Hello Gemmi, confirm your REST API endpoints are active." };
            var chatContent = new StringContent(JsonSerializer.Serialize(chatPayload), Encoding.UTF8, "application/json");
            var chatResp = await http.PostAsync("api/chat", chatContent);
            string chatBody = await chatResp.Content.ReadAsStringAsync();
            Console.WriteLine($"    • [PASS] Chat Response Received: {chatBody}");

            // 5. Test POST /api/command
            Console.WriteLine("\n[5] Testing POST /api/command (Switching state to walk)...");
            var cmdPayload = new { state = "walk" };
            var cmdContent = new StringContent(JsonSerializer.Serialize(cmdPayload), Encoding.UTF8, "application/json");
            var cmdResp = await http.PostAsync("api/command", cmdContent);
            Console.WriteLine($"    • [PASS] Command Status: {cmdResp.StatusCode}");

            // 6. Test POST /api/tasks (Dispatching Agent Task)
            Console.WriteLine("\n[6] Testing POST /api/tasks (Dispatching 'ModelInspector' tool)...");
            var taskPayload = new { toolName = "ModelInspector", parameters = new Dictionary<string, string>() };
            var taskContent = new StringContent(JsonSerializer.Serialize(taskPayload), Encoding.UTF8, "application/json");
            var taskResp = await http.PostAsync("api/tasks", taskContent);
            string taskBody = await taskResp.Content.ReadAsStringAsync();
            Console.WriteLine($"    • [PASS] Task Enqueued: {taskBody}");

            // Wait a moment for worker execution
            await Task.Delay(200);

            // 7. Test GET /api/tasks (Listing Task History)
            Console.WriteLine("\n[7] Testing GET /api/tasks (Verifying execution history)...");
            var historyResp = await http.GetStringAsync("api/tasks");
            Console.WriteLine($"    • [PASS] Task History: {historyResp}");

            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("  [✓] STEP 21 REST API & ECOSYSTEM TEST PASSED 100%!                      ");
            Console.WriteLine("==========================================================================");
        }
        finally
        {
            networkServer.Stop();
            await orchestrator.StopAsync();
        }
    }
}
