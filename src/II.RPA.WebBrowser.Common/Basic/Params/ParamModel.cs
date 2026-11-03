using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using System;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using II.RPA.Logging;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamModel
    {
        private static ILogger logger = Logger.OfContext();

        public RunParamModel RunParam { get; set; }
        public int TabId { get; set; } = -1;
        public string ElementId { get; set; }
        public List<string> elementIds { get; set; }
        public bool IsCloseAlert { get; set; }
        public int Timeout
        {
            get
            {
                int value = RunParam.Timeout;
                logger.LogDebug($"get timeout:{value}");
                return value;
            }
        }
    }
}
