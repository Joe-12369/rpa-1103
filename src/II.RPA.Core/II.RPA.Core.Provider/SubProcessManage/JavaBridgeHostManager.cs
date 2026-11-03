using II.RPA.Core.Common;
using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace II.RPA.Core.Provider.SubProcessManage
{
    public class JavaBridgeHostManager
    {
        private readonly ILogger m_Logger;
        private readonly List<int> m_JavaHostPids = new List<int>();
        public JavaBridgeHostManager(ILogger aLogger)
        {
            m_Logger = aLogger;
        }
    }
}
