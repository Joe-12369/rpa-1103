using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    public enum InitScripeStateType
    {
        None = 0,
        Initializing = 1,
        Complete = 2
    }

    public interface IBrowserConnectionNew
    {
        bool IsConnected { get; set; }
        InitScripeStateType InitScripeState { get; set; }
        PipeResponse PipeResponse { get; }
        void Start();
        void Stop();
        PipeResponse Send(eGuiType guiType, string command, JObject @params, int timeout = 5000);
        bool TestConnection();
        bool TestConnection(eGuiType guiType, int timeOut = 300);
        string CrxVersion { get; set; }
        int ConnectTimeout { get; set; }
        bool CrxIsLatestOrNewer { get; }
    }
    public abstract class BrowserCrxComparator
    {
        private static ILogger logger = Logger.OfContext();
        public virtual string LatestCrxVersion_base4 => "4.0.1.191";
        private bool _IsLatestOrNewer = false;
        /// <summary>
        /// 判断当前版本是否是最新版本。
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <returns></returns>
        public virtual bool IsLatestOrNewer(string currentVersion)
        {
            if (_IsLatestOrNewer)
            {
                return _IsLatestOrNewer;
            }
            if (string.IsNullOrEmpty(currentVersion))
            {
                return true;
            }

            if (currentVersion.StartsWith("2."))
            {
                logger.LogInformation($"CurrentVersion：{currentVersion}");
                _IsLatestOrNewer = true;
                return _IsLatestOrNewer;
            }
            string baseline = LatestCrxVersion_base4;

            logger.LogInformation($"CurrentVersion：{currentVersion}，BaselineVersion：{baseline}");
            if (Version.TryParse(currentVersion, out var vCurrent) && Version.TryParse(baseline, out var vBase))
            {
                _IsLatestOrNewer = vCurrent >= vBase;

            }
            else
            {
                _IsLatestOrNewer = string.Equals(currentVersion, baseline, StringComparison.OrdinalIgnoreCase);
            }
            return _IsLatestOrNewer;
        }
    }
}
