
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.NativeMessageingStartup;

namespace II.RPA.NativeMessagingHost360
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Logger.ConfigWithRole(LoggingRoles.Default);
            HostStartup startup = new HostStartup();
            startup.Startup(args);
        }
    }
}
