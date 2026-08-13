using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gemmi.Core;

public enum AgentTaskStatus
{
    Pending,
    Executing,
    Succeeded,
    Failed,
    Cancelled
}

public class AgentToolResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static AgentToolResult Ok(string message = "Success", object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static AgentToolResult Fail(string error) =>
        new() { Success = false, Message = error };
}

public class AgentTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public AgentToolResult? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AgentToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Func<Dictionary<string, object>, CancellationToken, Task<AgentToolResult>> Handler { get; set; }

    public AgentToolDefinition(string name, string description, Func<Dictionary<string, object>, CancellationToken, Task<AgentToolResult>> handler)
    {
        Name = name;
        Description = description;
        Handler = handler;
    }
}

/// <summary>
/// Deterministic Agent Task Orchestrator and Tool Dispatcher.
/// Coordinates background task execution, tool dispatching, and state management.
/// </summary>
public class GemmiAgentOrchestrator : IDisposable
{
    private readonly ConcurrentQueue<AgentTask> _taskQueue = new();
    private readonly ConcurrentDictionary<string, AgentToolDefinition> _tools = new();
    private readonly List<AgentTask> _taskHistory = new();
    private readonly object _historyLock = new();

    private CancellationTokenSource? _workerCts;
    private Task? _workerLoopTask;
    private bool _isRunning;

    public event Action<AgentTask>? OnTaskStarted;
    public event Action<AgentTask>? OnTaskCompleted;
    public event Action<AgentTask, string>? OnTaskFailed;

    public bool IsRunning => _isRunning;
    public int PendingTaskCount => _taskQueue.Count;
    public IReadOnlyList<AgentTask> History
    {
        get
        {
            lock (_historyLock)
            {
                return _taskHistory.ToArray();
            }
        }
    }

    public GemmiAgentOrchestrator()
    {
        RegisterDefaultTools();
    }

    private void RegisterDefaultTools()
    {
        RegisterTool("Ping", "Health check tool that returns engine status", (parameters, ct) =>
        {
            return Task.FromResult(AgentToolResult.Ok("Engine healthy", new { Timestamp = DateTime.UtcNow }));
        });

        RegisterTool("LogObservation", "Appends an environmental observation to memory buffer", (parameters, ct) =>
        {
            var text = parameters.TryGetValue("text", out var t) ? t?.ToString() ?? "" : "No text";
            return Task.FromResult(AgentToolResult.Ok($"Logged: {text}"));
        });
    }

    public void RegisterTool(string name, string description, Func<Dictionary<string, object>, CancellationToken, Task<AgentToolResult>> handler)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(handler);

        _tools[name] = new AgentToolDefinition(name, description, handler);
    }

    public AgentTask EnqueueTask(string name, string toolName, Dictionary<string, object>? parameters = null)
    {
        var task = new AgentTask
        {
            Name = name,
            ToolName = toolName,
            Parameters = parameters ?? new Dictionary<string, object>()
        };

        _taskQueue.Enqueue(task);
        return task;
    }

    public void Start(CancellationToken externalCancellationToken = default)
    {
        if (_isRunning) return;

        _workerCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        _isRunning = true;
        _workerLoopTask = Task.Run(() => WorkerLoopAsync(_workerCts.Token));
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;
        _isRunning = false;

        if (_workerCts != null)
        {
            _workerCts.Cancel();
            if (_workerLoopTask != null)
            {
                try
                {
                    await _workerLoopTask;
                }
                catch (OperationCanceledException) { }
            }
            _workerCts.Dispose();
            _workerCts = null;
        }
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_taskQueue.TryDequeue(out var task))
            {
                task.StartedAt = DateTime.UtcNow;
                task.Status = AgentTaskStatus.Executing;
                OnTaskStarted?.Invoke(task);

                if (_tools.TryGetValue(task.ToolName, out var toolDef))
                {
                    try
                    {
                        using var taskTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, taskTimeoutCts.Token);

                        var result = await toolDef.Handler(task.Parameters, linkedCts.Token);
                        task.Result = result;
                        task.CompletedAt = DateTime.UtcNow;
                        task.Status = result.Success ? AgentTaskStatus.Succeeded : AgentTaskStatus.Failed;

                        if (result.Success)
                        {
                            OnTaskCompleted?.Invoke(task);
                        }
                        else
                        {
                            OnTaskFailed?.Invoke(task, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        task.Status = AgentTaskStatus.Failed;
                        task.CompletedAt = DateTime.UtcNow;
                        task.Result = AgentToolResult.Fail(ex.Message);
                        OnTaskFailed?.Invoke(task, ex.Message);
                    }
                }
                else
                {
                    task.Status = AgentTaskStatus.Failed;
                    task.CompletedAt = DateTime.UtcNow;
                    task.Result = AgentToolResult.Fail($"Tool '{task.ToolName}' not registered");
                    OnTaskFailed?.Invoke(task, $"Tool '{task.ToolName}' not registered");
                }

                lock (_historyLock)
                {
                    _taskHistory.Add(task);
                    if (_taskHistory.Count > 100) _taskHistory.RemoveAt(0);
                }
            }
            else
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}
