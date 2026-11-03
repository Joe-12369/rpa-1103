#nullable disable

using II.RPA.Logging.Configurations;
using System.Runtime.CompilerServices;

namespace II.RPA.Core.Log;

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
    void LogFatal(string aMessage, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);
    void LogFatal(string aMessage, Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);
    void LogFatal(Exception anException, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string aCallerFilePath = null);
    void LogCommunication(string aMessage, Exception anException, StreamScope aStreamScope, [CallerFilePath] string aCallerFilePath = null);
    void LogCommunication(string aMessage, StreamScope aStreamScope, [CallerFilePath] string aCallerFilePath = null);
    void LogFFmpeg(string aMessage, [CallerFilePath] string aCallerFilePath = null);
    void LogZBrain(string aMessage, [CallerFilePath] string aCallerFilePath = null);
    void LogExecutor(string aMessage, [CallerFilePath] string aCallerFilePath = null);
    void LogInterpreter(string aMessage, [CallerFilePath] string aCallerFilePath = null);
    void LogZdesktop(string aMessage, [CallerFilePath] string aCallerFilePath = null);
    void LogMCPAgent(string aMessage, [CallerFilePath] string aCallerFilePath = null);
}
