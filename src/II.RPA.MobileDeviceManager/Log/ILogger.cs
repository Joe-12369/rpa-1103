using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Log
{
    public interface ILogger
    {
        void LogTrace(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogTrace(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogDebug(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogDebug(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogInfo(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogInfo(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogWarning(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogWarning(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogError(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogError(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);

        void LogError(Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);
    }
}
