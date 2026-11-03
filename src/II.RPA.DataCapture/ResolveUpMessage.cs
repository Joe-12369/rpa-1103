using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSHTML;
using Newtonsoft.Json.Linq;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace II.RPA.DataCapture
{
    //Test 123456
    public class ResolveUpMessage
    {
        public void ResolveMessageToEngine(JObject jsonText)
        {
            TransferExecutorMessage nTransferToEngine = new TransferExecutorMessage();
            nTransferToEngine.FromExecutorToEngineMessage(jsonText);
        }
        public void ResovleMessageToConfigure(string strText)
        {
            Task.Run(() =>
            {
                TransferExecutorMessage nTransferToConfigure = new TransferExecutorMessage();
                nTransferToConfigure.FromExecutorToConfigureMessage(strText);
            });
        }
        public void ResolveMessage(string strText)
        {

            PublicConfig.LogTrace(string.Format("Document Execute Message {0}", strText));
            JObject jsonText = JObject.Parse(strText);
            if (jsonText.ContainsKey("from") == true && jsonText["from"].ToString() != "engine")
            {
                if (jsonText["to"].ToString() == "engine")
                {
                    ResolveMessageToEngine(jsonText);
                }
                else if (jsonText["to"].ToString() == "configure")
                {
                    try
                    {
                        string strRet = JsonConvert.SerializeObject(jsonText["data"]["params"]);
                        if (strRet == "[\"规则不能为空数组\"]")
                        {
                            PublicConfig.ReturnErrorInfo(string.Format("规则不能为空数组!"), 13);
                            return;
                        }
                        else
                        {
                            try
                            {
                                JArray rulesArray = JArray.Parse(strRet);
                                if (rulesArray.Count > 0)
                                {
                                    JObject objListDesc = JObject.Parse(rulesArray[0].ToString());
                                    if (objListDesc.ContainsKey("listDesc"))
                                    {
                                        JObject rulesObj = JObject.Parse(objListDesc["listDesc"].ToString());
                                        if (rulesObj.ContainsKey("fieldRules"))
                                        {
                                            PublicConfig.gbRulesGet = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception EX)
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    ResovleMessageToConfigure(strText);
                }
            }   

        }
    }
}
