using II.RPA.Logging;
using II.RPA.Logging.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;

namespace II.RPA.Core.Log
{
    public class AdaptedLogger : MarshalByRefObject, ILogger
    {
        public AdaptedLogger()
        {
            string startup =
                $"===============================Core Server Startup Begin==================================";
            string logo = @"
              _____                      _____                          
             / ____|                    / ____|                         
            | |     ___  _ __ ___      | (___   ___ _ ____   _____ _ __ 
            | |    / _ \| '__/ _ \      \___ \ / _ \ '__\ \ / / _ \ '__|
            | |___| (_) | | |  __/      ____) |  __/ |   \ V /  __/ |   
             \_____\___/|_|  \___|     |_____/ \___|_|    \_/ \___|_|   ";

            var logger = Logger.OfContext();
            logger.LogInformation($"{Environment.NewLine}{startup}{Environment.NewLine}{logo}{Environment.NewLine}");
            LogInfo($"Current Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public void LogDebug(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogDebug(aMessage);
        }

        public void LogDebug(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogDebug(anException, aMessage);
        }

        public void LogError(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogError(aMessage);
        }

        public void LogError(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogError(anException, aMessage);
        }

        public void LogFatal(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogCritical(aMessage);
        }

        public void LogFatal(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogCritical(anException, aMessage);
        }

        public void LogInfo(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogInformation(aMessage);
        }

        public void LogInfo(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogInformation(anException, aMessage);
        }

        public void LogTrace(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogDebug(aMessage);
        }

        public void LogTrace(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogDebug(anException, aMessage);
        }

        public void LogWarning(string aMessage, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogWarning(aMessage);
        }

        public void LogWarning(string aMessage, Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogWarning(anException, aMessage);
        }

        public void LogError(Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogError(anException);
        }

        public void LogFatal(Exception anException, [CallerMemberName] string aCallerMemberName = null, [CallerFilePath] string aCallerFilePath = null)
        {
            Logger.OfContext(aCallerFilePath).LogCritical(anException);
        }
        public void LogCommunication(string aMessage, Exception anException,StreamScope aStreamScope, [CallerFilePath] string aCallerFilePath = null)
        {
            var logger = Logger.OfContext(aCallerFilePath);
            logger.WithStreamScope(logger => { Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, anException, aMessage); }, aStreamScope);
        }
        public void LogCommunication(string aMessage, StreamScope aStreamScope, [CallerFilePath] string aCallerFilePath = null)
        {
            var logger = Logger.OfContext(aCallerFilePath);
            logger.WithStreamScope(logger => { logger.LogInformation(aMessage); }, aStreamScope);
        }
        public void LogFFmpeg(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            var logger = Logger.OfContext(aCallerFilePath);
            logger.WithFFmpeg(logger => { logger.LogInformation(aMessage); });
        }
        public void LogZBrain(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            LogWorker(aMessage, "z-brain");
        }
        public void LogExecutor(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            LogWorker(aMessage, "executor");
        }
        public void LogInterpreter(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            LogWorker(aMessage, "interpreter");
        }
        public void LogWorker(string aMessage, string aWorkName, [CallerFilePath] string aCallerFilePath = null)
        {
            var logger = Logger.OfContext(aCallerFilePath);
            logger.WithWorker(logger => { logger.LogInformation("{s}", aMessage); }, aWorkName);
        }
        public void LogZdesktop(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            LogWorker(aMessage, "Zdesktop");
        }
        public void LogMCPAgent(string aMessage, [CallerFilePath] string aCallerFilePath = null)
        {
            LogWorker(aMessage, "mcp-agent");
        }
    }
}

