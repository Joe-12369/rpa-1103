using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Tests
{
    internal sealed class RecordingLogger : ILogger
    {
        public List<LogRecord> Records { get; } = new List<LogRecord>();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = formatter == null ? state?.ToString() : formatter(state, exception);
            Records.Add(new LogRecord(logLevel, exception, message));
        }
    }

    internal sealed class LogRecord
    {
        public LogRecord(LogLevel level, Exception exception, string message)
        {
            Level = level;
            Exception = exception;
            Message = message;
        }

        public LogLevel Level { get; }

        public Exception Exception { get; }

        public string Message { get; }
    }
}
