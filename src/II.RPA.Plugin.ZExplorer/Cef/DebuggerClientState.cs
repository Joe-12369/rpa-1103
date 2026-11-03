using System;
using System.Diagnostics;
using System.Linq;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    [Serializable]
    public struct DebuggerClientState
    {
        internal DebuggerClientState(ChromeDebugProtocolClientConnector client, CefResult<CefUnit> connectionStartupResult)
        {
            this.IsStarted = (client != null);
            //this.HasOccupiedEndpoints = client != null && client.Endpoints.Any() && client.HasOccupiedEndpoints();
            this.HasOccupiedEndpoints = false;
            this.CefHostProcessId = Process.GetCurrentProcess().Id;
            this.ConnectionStartupResult = connectionStartupResult;
        }

        public bool IsStarted { get; }

        public bool HasOccupiedEndpoints { get; }

        public int CefHostProcessId { get; }

        public CefResult<CefUnit> ConnectionStartupResult { get; }
    }
}
