using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using II.RPA.Core.Talker.Processes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace II.RPA.Core.Talker
{
    /// <summary>
    /// Runs one MCP Agent process and reports its progress through one explicit callback.
    /// The internal channel only serializes stdout/stderr produced by different process threads.
    /// </summary>
    public sealed class MCPAgentRunner : IDisposable
    {
        private static readonly Regex ResultRegex = new(
            @"^<<RESULT>>(.*?)<<RESULT>>$",
            RegexOptions.Compiled);

        private readonly ILogger _logger;
        private readonly IRpaRuntimeContext _runtimeContext;
        private readonly HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public MCPAgentRunner(ILogger logger, IRpaRuntimeContext runtimeContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeContext = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
        }

        public async Task RunAsync(
            IReadOnlyDictionary<string, string> configuration,
            Func<JObject, Task> reportProgressAsync,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(reportProgressAsync);

            var userToken = GetRequired(configuration, "II_USER_TOKEN");
            var agentHost = GetRequired(configuration, "II_AGENT_HOST");
            var agentInputJson = GetRequired(configuration, "II_AGENT_INPUT");
            var agentInput = JObject.Parse(agentInputJson);
            var workflowRunId = agentInput.Value<string>("workflow_run_id") ?? string.Empty;
            var toolCallBatch = agentInput.Value<string>("tool_call_batch") ?? string.Empty;

            _client.BaseAddress = new Uri(new Uri(agentHost + "/"), "front/1.0.0/");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userToken);

            var output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            var process = new MCPAgentProcess(
                _logger,
                _runtimeContext,
                line => output.Writer.TryWrite(line),
                line => output.Writer.TryWrite(line));
            process.Environment["II_USER_TOKEN"] = userToken;
            process.Environment["II_AGENT_HOST"] = agentHost;
            process.Environment["II_AGENT_INPUT"] = agentInputJson;
            process.Environment["MCP_USE_ANONYMIZED_TELEMETRY"] = "false";

            var state = new ProcessingState();
            var processingTask = ProcessOutputAsync(
                output.Reader,
                state,
                workflowRunId,
                toolCallBatch,
                reportProgressAsync,
                cancellationToken);

            try
            {
                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError("启动 MCP Agent 失败", ex);
                    await reportProgressAsync(CreatePreparationError(ex.Message)).ConfigureAwait(false);
                    return;
                }

                try
                {
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await process.StopAsync().ConfigureAwait(false);
                    throw;
                }
            }
            finally
            {
                output.Writer.TryComplete();
                await processingTask.ConfigureAwait(false);
            }

            if (!state.Started)
            {
                await reportProgressAsync(CreatePreparationError(
                    $"准备过程中异常退出, 退出码: {process.LastExitCode?.ToString() ?? "unknown"}"))
                    .ConfigureAwait(false);
            }

            if (!state.Completed && !state.Failed)
            {
                await ReportEventSafelyAsync(new JObject
                {
                    ["event"] = "error",
                    ["message"] = "MCP程序已退出"
                }, workflowRunId, toolCallBatch, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessOutputAsync(
            ChannelReader<string> output,
            ProcessingState state,
            string workflowRunId,
            string toolCallBatch,
            Func<JObject, Task> reportProgressAsync,
            CancellationToken cancellationToken)
        {
            await foreach (var line in output.ReadAllAsync())
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = ResultRegex.Match(line);
                if (!match.Success)
                {
                    if (!state.Started)
                    {
                        await reportProgressAsync(new JObject
                        {
                            ["status"] = "preparing",
                            ["msg"] = line,
                            ["text"] = "0"
                        }).ConfigureAwait(false);
                    }
                    continue;
                }

                JObject agentEvent;
                try
                {
                    agentEvent = JObject.Parse(match.Groups[1].Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"无法解析 MCP Agent 输出: {line}", ex);
                    continue;
                }

                var eventName = agentEvent.Value<string>("event");
                if (string.Equals(eventName, "started", StringComparison.OrdinalIgnoreCase))
                {
                    if (!state.Started)
                    {
                        state.Started = true;
                        await reportProgressAsync(new JObject
                        {
                            ["status"] = "prepareSuccess",
                            ["msg"] = string.Empty,
                            ["text"] = "0"
                        }).ConfigureAwait(false);
                    }
                    continue;
                }

                state.Completed |= string.Equals(eventName, "done", StringComparison.OrdinalIgnoreCase);
                state.Failed |= string.Equals(eventName, "error", StringComparison.OrdinalIgnoreCase);
                await ReportEventSafelyAsync(
                    agentEvent,
                    workflowRunId,
                    toolCallBatch,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReportEventSafelyAsync(
            JObject agentEvent,
            string workflowRunId,
            string toolCallBatch,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo(JsonConvert.SerializeObject(agentEvent));
                var payload = new JObject
                {
                    ["workflow_run_id"] = workflowRunId,
                    ["tool_call_batch"] = toolCallBatch,
                    ["result"] = agentEvent
                };
                using var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");
                using var response = await _client.PostAsync(
                    "mcp/tool-call/result",
                    content,
                    cancellationToken).ConfigureAwait(false);
                _logger.LogInfo($"Response with code: {response.StatusCode}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("上报 MCP Agent 事件失败", ex);
            }
        }

        private static JObject CreatePreparationError(string message)
        {
            return new JObject
            {
                ["status"] = "prepareError",
                ["msg"] = message,
                ["text"] = "2"
            };
        }

        private static string GetRequired(
            IReadOnlyDictionary<string, string> configuration,
            string key)
        {
            if (!configuration.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"MCP Agent configuration is missing {key}.", nameof(configuration));
            }
            return value;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private sealed class ProcessingState
        {
            public bool Started { get; set; }

            public bool Completed { get; set; }

            public bool Failed { get; set; }
        }
    }
}
