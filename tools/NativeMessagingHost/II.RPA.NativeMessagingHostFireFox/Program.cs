using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using II.RPA.NativeMessageingStartup;
using II.RPA.NativeMessagingHostFireFox;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace II.RPA.NativeMessagingHostFireFox
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
