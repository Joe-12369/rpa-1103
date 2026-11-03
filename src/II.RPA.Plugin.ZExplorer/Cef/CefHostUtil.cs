using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.ZExplorer.Cef
{
    internal sealed class CefHostUtil
    {
        internal static CefResult<int[]> TryGetDebuggerPortFromCommandLine()
        {
            log.LogDebug("CEFHost: Get debugger port from cmd args: '" + Environment.CommandLine + "'");
            Match match = Regex.Match(Environment.CommandLine, "--remote-debugging-port=(\\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int num = -1;
            if (match.Groups.Count == 2 && int.TryParse(match.Groups[1].Value, out num))
            {
                return CefResult.Ok<int[]>(new int[]
                {
                    num
                });
            }
            return CefResult.Unknown<int[]>();
        }

        private static readonly ILogger log = Logger.OfContext();
    }
}
