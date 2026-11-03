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
    class TransferConfigureMessage
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public void PostExitMessage(string strInfo, bool bError)
        {
            PublicConfig.gbCloseForm = true;
            JObject obj = new JObject();
            obj["code"] = 32;
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
            obj["code"] = 32;
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
        public void FromConfigureToEngineMessage(JObject jsonText)
        {
            //if (PublicConfig.gbCloseForm == false)
            {
                PublicConfig.LogTrace(string.Format("Document Configure {0}", jsonText.ToString()));
                string strData = jsonText["data"].ToString();
                if (strData == "injectExecutor")
                {

                }
                else
                {
                    try
                    {
                        JObject objData = JObject.Parse(jsonText["data"].ToString());
                        if (objData.ContainsKey("fieldRules") == true)
                        {
                            PostExitMessage(objData, false);
                        }
                        else
                        {
                            try
                            {
                                if (PublicConfig.gbCloseForm == false)
                                {
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
                            catch (Exception ex1)
                            {
                                PostExitMessage(objData, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        PostExitMessage(jsonText["data"].ToString(), false);
                    }
                }
            }
        }
        public void IEFromConfigureToExecutorMessage(JObject jsonText)
        {
            PublicConfig.gUpMutex2.WaitOne();
            try
            {
                PublicConfig.gUpMutexInit.WaitOne();
                if (PublicConfig.ghtmlUpDoc != null)
                {
                    IHTMLElement htmlElementUp = PublicConfig.ghtmlUpDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                    if (htmlElementUp != null)
                    {
                        htmlElementUp.innerText = JsonConvert.SerializeObject(jsonText);
                        IDOMEvent event1 = PublicConfig.ghtmlUpDoc.createEvent("HTMLEvents");
                        event1.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                        bool bRet = PublicConfig.ghtmlUpDoc.dispatchEvent(event1);
                        PublicConfig.LogTrace(string.Format("Document Execute DispatchEvent {0}", bRet));
                        PublicConfig.LogTrace(string.Format("Document htmlElementUp.innerText 2 {0} \r\nstrText:{1}", htmlElementUp.innerText, JsonConvert.SerializeObject(jsonText)));
                    }
                }
                PublicConfig.gUpMutexInit.ReleaseMutex();
            }
            catch (Exception ex)
            {
                PublicConfig.gUpMutexInit.ReleaseMutex();
            }
            PublicConfig.gUpMutex2.ReleaseMutex();
        }
    }
}
