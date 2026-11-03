using II.RPA.Logging.Configurations;
using II.RPA.Logging.Definitions;
using II.RPA.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace II.RPA.MobileDeviceManager.Log
{
    public static class LogScopeExtension
    {
        public static void LogDebug(
           this Microsoft.Extensions.Logging.ILogger logger,
           Exception ex,
           string message = null,
           params object[] parameters)
        {
            logger.LogDebug(EventIds.DebugDefault, ex, message.NotNullOr(ex.Message), parameters);
        }

        public static void LogInformation(
            this Microsoft.Extensions.Logging.ILogger logger,
            Exception ex,
            string message = null,
            params object[] parameters)
        {
            logger.LogInformation(EventIds.InfoDefault, ex, message.NotNullOr(ex.Message), parameters);
        }

        public static void LogWarning(
           this Microsoft.Extensions.Logging.ILogger logger,
           Exception ex,
           string message = null,
           params object[] parameters)
        {
            logger.LogWarning(EventIds.WarningDefault, ex, message.NotNullOr(ex.Message), parameters);
        }

        public static void LogError(
           this Microsoft.Extensions.Logging.ILogger logger,
           Exception ex,
           string message = null,
           params object[] parameters)
        {
            logger.LogError(EventIds.ErrorDefault, ex, message.NotNullOr(ex.Message), parameters);
        }

        public static void LogCritical(
           this Microsoft.Extensions.Logging.ILogger logger,
           Exception ex,
           string message = null,
           params object[] parameters)
        {
            logger.LogCritical(EventIds.ErrorDefault, ex, message.NotNullOr(ex.Message), parameters);
        }

        public static void WithStreamScope(
            this Microsoft.Extensions.Logging.ILogger logger,
            Action<Microsoft.Extensions.Logging.ILogger> logAction,
            string mode = null,
            string stage = null,
            string action = null,
            string requestId = null)
        {
            using (var scope = logger.BeginScope(
                new Dictionary<string, object>
                {
                    [StreamScopeDefs.ScopeNameKey] = "Stream",
                    [StreamScopeDefs.IsAsyncKey] = mode ?? StreamScopeDefs.UnknownValue,
                    [StreamScopeDefs.ClientKey] = stage ?? StreamScopeDefs.UnknownValue,
                    [StreamScopeDefs.ActionKey] = action ?? StreamScopeDefs.UnknownValue,
                    [StreamScopeDefs.RequestIdKey] = requestId ?? StreamScopeDefs.UnknownValue,
                }))
            {
                logAction(logger);
            }
        }

        public static void WithStreamScope(
            this Microsoft.Extensions.Logging.ILogger logger,
            Action<Microsoft.Extensions.Logging.ILogger> logAction,
            StreamScope streamScope)
        {
            logger.WithStreamScope(
                logAction,
                streamScope.IsAsync,
                streamScope.Client,
                streamScope.Action,
                streamScope.RequestId);
        }

    }
}
