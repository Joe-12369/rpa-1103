using II.RPA.Logging;
using II.RPA.Logging.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;

namespace II.RPA.MobileDeviceManager.Log
{
    public class AdaptedLogger : MarshalByRefObject, ILogger
    {
        public AdaptedLogger()
        {
            string startup =
                $"===============================Mobile Server Startup Begin==================================";
            string logo = "";

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

    }
}
