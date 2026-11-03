using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSHTML;
using Newtonsoft.Json.Linq;
using II.RPA.WinAutomation.CommonUtility;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.InteropServices;

namespace II.RPA.DataCapture
{
    public class ResolveDownMessage
    {
        public void ResolveMessageToEngine(JObject jsonText)
        {
            TransferConfigureMessage nTranferToEngine = new TransferConfigureMessage();
            nTranferToEngine.FromConfigureToEngineMessage(jsonText);
        }
        public void ResolveMessageExecutor(JObject jsonText)
        {
            Task.Run(() =>
            {
                TransferConfigureMessage nTransferToExecutor = new TransferConfigureMessage();
                nTransferToExecutor.IEFromConfigureToExecutorMessage(jsonText);
            });
        }
        public void ResolveMessage(string strText)
        {

            PublicConfig.LogTrace(string.Format("Document Configure Message {0}", strText));
            JObject jsonText = JObject.Parse(strText);
            if (jsonText.ContainsKey("from") == true)
            {
                if (jsonText["to"].ToString() == "engine")
                {
                    ResolveMessageToEngine(jsonText);
                }
                else if (jsonText["to"].ToString() == "executor")
                {
                    ResolveMessageExecutor(jsonText);
                }
            }                
        }
    }
}
