using II.RPA.Core.Common;
using II.RPA.Core.Log;
using System;
using System.Diagnostics;

namespace II.RPA.Core.Provider.MobileHostProvider
{
    public class MobileHostProvider
    {
        private readonly ILogger m_Logger;
        public MobileHostProvider(ILogger aLogger)
        {
            m_Logger = aLogger;
        }
        public void MobileHostLoader(string channelName)
        {
            var clintType = "1";
            if (Global.ClientIDString.ToLower() != "z-factory")
            {
                clintType = "2";
            }
            string paraStr = $"-c {clintType} -p {Global.CurrentPId} -n {channelName}";
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = paraStr,
                    FileName = "II.RPA.MobileProvider.exe",
                    UseShellExecute = false,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };
            process.Exited += Process_Exited;
            process.Start();

        }
        private void Process_Exited(object sender, EventArgs e)
        {
            m_Logger.LogInfo($"MobileProvider exit!");
        }
    }
}
