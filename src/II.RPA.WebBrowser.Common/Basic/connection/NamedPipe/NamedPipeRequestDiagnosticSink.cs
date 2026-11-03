using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace II.RPA.WebBrowser.Common.Basic.connection.NamedPipe
{
    internal sealed class NamedPipeRequestDiagnosticSink : INamedPipeRequestDiagnosticSink
    {
        private readonly ILogger _logger = Logger.OfContext();

        public void Write(
            NamedPipeDiagnosticLevel level,
            string message,
            Exception exception)
        {
            switch (level)
            {
                case NamedPipeDiagnosticLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case NamedPipeDiagnosticLevel.Error:
                    _logger.LogError(message);
                    break;
                default:
                    _logger.LogDebug(message);
                    break;
            }
        }
    }
}
