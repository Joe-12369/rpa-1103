using MSHTML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.DataCapture
{
    class TransferExecutorMessage
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public bool bClose = false;
        public void PostExitMessage(string strInfo, bool bError)
        {
            PublicConfig.gbCloseForm = true;
            JObject obj = new JObject();
            obj["code"] = 33;
            if (bError)
            {
                obj["info"] = strInfo;
                obj["retCode"] = "ERROR";
            }
            else
            {
                obj["info"] = strInfo;
                obj["retCode"] = "SUCCEED";
            }
            PublicConfig.gstrCapture = JsonConvert.SerializeObject(obj);
            PostMessage(PublicConfig.gIntPtrWnd, PublicConfig.WM_WINFORM_CLOSE, (IntPtr)0, (IntPtr)0);
        }
        public void PostExitMessage(JObject objData, bool bError)
        {
            PublicConfig.gbCloseForm = true;
            JObject obj = new JObject();
            obj["code"] = 33;
            if (bError)
            {
                obj["info"] = objData;
                obj["retCode"] = "ERROR";
            }
            else
            {
                obj["info"] = objData;
                obj["retCode"] = "SUCCEED";
            }
            PublicConfig.gstrCapture = JsonConvert.SerializeObject(obj);
            PostMessage(PublicConfig.gIntPtrWnd, PublicConfig.WM_WINFORM_CLOSE, (IntPtr)0, (IntPtr)0);
        }
        public void FromExecutorToConfigureMessage(string strText)
        {
            PublicConfig.gDownMutex2.WaitOne();
            try
            {
                if (PublicConfig.ghtmlDownDoc != null)
                {
                    IHTMLElement htmlElementDown = PublicConfig.ghtmlDownDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                    if (htmlElementDown != null)
                    {
                        htmlElementDown.innerText = strText;
                        IDOMEvent event2 = PublicConfig.ghtmlDownDoc.createEvent("HTMLEvents");
                        event2.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                        bool bRet = PublicConfig.ghtmlDownDoc.dispatchEvent(event2);
                        PublicConfig.LogTrace(string.Format("Document Configure DispatchEvent {0}", bRet));
                        PublicConfig.LogTrace(string.Format("Document htmlElementDown.innerText {0} \r\nstrText:{1}", htmlElementDown.innerText, strText));
                    }
                }
            }
            catch (Exception ex)
            {

            }
            PublicConfig.gDownMutex2.ReleaseMutex();
        }
        public void FromExecutorToEngineMessage(JObject jsonText)
        {
            //if (PublicConfig.gbCloseForm == false)
            {
                PublicConfig.LogTrace(string.Format("Document Execute 1 {0}", jsonText.ToString()));
                try
                {
                    string strData = jsonText["data"].ToString();
                    if (strData == "injectExecutor")
                    {

                    }
                    else
                    {
                        PublicConfig.LogTrace(string.Format("Document Execute 2 {0}", jsonText.ToString()));
                        JObject objData = JObject.Parse(jsonText["data"].ToString());
                        try
                        {
                            JObject method = JObject.Parse(jsonText["data"].ToString());
                            if (method["method"].ToString() == "fail")
                            {
                                JArray array = JArray.Parse(method["payload"].ToString());
                                if (array.Count > 0)
                                {
                                    PostExitMessage(array[0].ToString(), true);
                                }
                                else
                                {
                                    PostExitMessage(objData, true);
                                }
                            }
                            else
                            {
                                PostExitMessage(objData, false);
                            }
                        }
                        catch (Exception EX)
                        {
                            PostExitMessage(objData, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    PostExitMessage(jsonText["data"].ToString(), true);
                }
            }
        }
    }
}
