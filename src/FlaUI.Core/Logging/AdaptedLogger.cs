using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using System;

namespace FlaUI.Core.Logging
{
    /// <summary>
    /// 桥接FlaUI的日志和RPA的日志
    /// </summary>
    public class AdaptedLogger : ILogger
    {
        private static Microsoft.Extensions.Logging.ILogger logger
             = II.RPA.Logging.Logger.OfContext();

        public bool IsTraceEnabled { get; set; } = true;

        public bool IsDebugEnabled { get; set; } = true;

        public bool IsInfoEnabled { get; set; } = true;

        public bool IsWarnEnabled { get; set; } = true;

        public bool IsErrorEnabled { get; set; } = true;

        public bool IsFatalEnabled { get; set; } = true;

        public void Debug(string message, params object[] args)
        {
            logger.LogDebug(message, args);
        }

        public void Debug(string message, Exception exception, params object[] args)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, exception, message, args);
        }

        public void Error(string message, params object[] args)
        {
            logger.LogError(message, args);
        }

        public void Error(string message, Exception exception, params object[] args)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, exception, message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            logger.LogCritical(message, args);
        }

        public void Fatal(string message, Exception exception, params object[] args)
        {
            logger.LogCritical(EventIds.CriticalDefault, exception, message, args);
        }

        public void Info(string message, params object[] args)
        {
            logger.LogInformation(message, args);
        }

        public void Info(string message, Exception exception, params object[] args)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, exception, message, args);
        }

        public void Log(LogLevel logLevel, string message, params object[] args)
        {
            var level = logLevel == LogLevel.Trace ? Microsoft.Extensions.Logging.LogLevel.Debug :
                logLevel == LogLevel.Debug ? Microsoft.Extensions.Logging.LogLevel.Debug :
                logLevel == LogLevel.Info ? Microsoft.Extensions.Logging.LogLevel.Information :
                logLevel == LogLevel.Warn ? Microsoft.Extensions.Logging.LogLevel.Warning :
                logLevel == LogLevel.Error ? Microsoft.Extensions.Logging.LogLevel.Error :
                Microsoft.Extensions.Logging.LogLevel.Critical;

            var eventId = logLevel == LogLevel.Trace ? EventIds.DebugDefault :
                logLevel == LogLevel.Debug ? EventIds.DebugDefault :
                logLevel == LogLevel.Info ? EventIds.InfoDefault :
                logLevel == LogLevel.Warn ? EventIds.WarningDefault :
                logLevel == LogLevel.Error ? EventIds.ErrorDefault :
                EventIds.CriticalDefault;

            logger.Log(level, eventId, null, message, args);
        }

        public void Log(LogLevel logLevel, string message, Exception exception, params object[] args)
        {
            var level = logLevel == LogLevel.Trace ? Microsoft.Extensions.Logging.LogLevel.Debug :
                logLevel == LogLevel.Debug ? Microsoft.Extensions.Logging.LogLevel.Debug :
                logLevel == LogLevel.Info ? Microsoft.Extensions.Logging.LogLevel.Information :
                logLevel == LogLevel.Warn ? Microsoft.Extensions.Logging.LogLevel.Warning :
                logLevel == LogLevel.Error ? Microsoft.Extensions.Logging.LogLevel.Error :
                Microsoft.Extensions.Logging.LogLevel.Critical;

            var eventId = logLevel == LogLevel.Trace ? EventIds.DebugDefault :
                logLevel == LogLevel.Debug ? EventIds.DebugDefault :
                logLevel == LogLevel.Info ? EventIds.InfoDefault :
                logLevel == LogLevel.Warn ? EventIds.WarningDefault :
                logLevel == LogLevel.Error ? EventIds.ErrorDefault :
                EventIds.CriticalDefault;

            logger.Log(level, eventId, exception, message, args);
        }

        public void SetLevel(LogLevel level)
        {

        }

        public void Trace(string message, params object[] args)
        {
            logger.LogDebug(message, args);
        }

        public void Trace(string message, Exception exception, params object[] args)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, exception, message, args);
        }

        public void Warn(string message, params object[] args)
        {
            logger.LogWarning(message, args);
        }

        public void Warn(string message, Exception exception, params object[] args)
        {
            Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, exception, message, args);
        }
    }
}

