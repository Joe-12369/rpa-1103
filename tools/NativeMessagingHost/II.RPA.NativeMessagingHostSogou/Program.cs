using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.NativeMessageingStartup;
using System.Linq;

namespace II.RPA.NativeMessagingHostSogou
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.ConfigWithRole(LoggingRoles.Default);
            HostStartup startup = new HostStartup();
            if (args == null)
            {
                args = Enumerable.Empty<string>().ToArray();
            }
            startup.Startup(args.Append("allowMulti"));
        }
    }
}
