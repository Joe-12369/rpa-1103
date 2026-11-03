using II.RPA.Logging;
using II.RPA.Plugin.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public static class RuntimeMessageService
{
    private static readonly ILogger _logger = Logger.OfContext();
    private static IRuntimeWorkerService _messageService;
    public static event Action<RuntimeMessageModel> OnMessageReceived;
    public static IObservable<RuntimeMessageModel> IncomingStream { get; private set; }
    public static void Bridge(IRuntimeWorkerService messageService)
    {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        IncomingStream = _messageService.MessageStream;
        _messageService.MessageStream
            .Subscribe(
                msg =>
                {
                    _logger.LogDebug("Received message: {Action} {Payload}", msg.Action, msg.Payload);
                    OnMessageReceived?.Invoke(msg);
                },
                ex => Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Error in message stream"),
                () => _logger.LogInformation("Message stream completed"));
    }

    public static async Task SendMessageAsync(RuntimeMessageModel message)
    {
        if (_messageService == null)
            throw new InvalidOperationException("Bridge not initialized. Call Bridge() first.");

        try
        {
            await _messageService.SendMessageAsync(message);
            _logger.LogInformation($"Sent message: {message.Action}");
        }
        catch (Exception ex)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Failed to send message");
        }
    }

    public static void SendMessage(RuntimeMessageModel message)
    {
        if (_messageService == null)
            throw new InvalidOperationException("Bridge not initialized. Call Bridge() first.");

        try
        {
            _messageService.SendMessage(message);
            _logger.LogInformation($"Sent message: {message.Action}");
            _logger.LogInformation($"{message.Payload}");
        }
        catch (Exception ex)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "Failed to send message");
        }
    }
}
