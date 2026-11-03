using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Server.Security
{
    public class ServerSecurityProvider
    {
        public ClientEnum RunByClient
        {
            get;
            set;
        }
        public RunModeEnum RunMode
        {
            get;
            set;
        }
    }
    public enum RunModeEnum
    {
        DEBUG = 0,
        RELEASE = 1,
    }
    public enum ClientEnum
    {
        DEBUG = 0,
        RELEASE = 1,
    }
}
