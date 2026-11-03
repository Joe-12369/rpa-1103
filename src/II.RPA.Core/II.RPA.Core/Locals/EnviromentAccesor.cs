using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Core.Log;

namespace II.RPA.Core.Locals
{
    public class EnviromentAccesor : IEnviromentAccesor
    {
        private readonly ILogger logger;

        public void KillProcessAndChildren(
            int pid,
            bool ignoreRoot = false)
        {
            return;
        }
    }
}
