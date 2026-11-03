using MSHTML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.DataCapture
{
    class TransferEngineMessage
    {
        public void FromEngineToConfigureMessageByRules()
        {
            if (PublicConfig.ghtmlDownDoc != null)
            {
                IHTMLElement htmlElementDown = PublicConfig.ghtmlDownDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                if (htmlElementDown != null)
                {
                    JObject obj = new JObject();
                    obj["from"] = "engine";
                    obj["to"] = "configure";
                    obj["data"] = JObject.Parse(PublicConfig.gstrListDesc);
                    htmlElementDown.innerText = JsonConvert.SerializeObject(obj);
                    IDOMEvent event1 = PublicConfig.ghtmlDownDoc.createEvent("HTMLEvents");
                    event1.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                    bool bRet1 = PublicConfig.ghtmlDownDoc.dispatchEvent(event1);
                    PublicConfig.LogTrace(string.Format("Document Configure dispatchEvent {0}", bRet1));
                    PublicConfig.LogTrace(string.Format("Document htmlElementDown.innerText 2 {0} \r\nstrText:{1}", htmlElementDown.innerText, JsonConvert.SerializeObject(obj)));
                }
            }
        }
        public void FromEngineToConfigureMessageBySetConfig()
        {
            if (PublicConfig.ghtmlDownDoc != null)
            {
                IHTMLElement htmlElementDown = PublicConfig.ghtmlDownDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                if (htmlElementDown != null)
                {
                    try
                    {
                        JObject listdesc = JObject.Parse(PublicConfig.gformCapture._strRule);
                        JObject obj = new JObject();
                        obj["from"] = "engine";
                        obj["to"] = "configure";
                        JObject obj_rules = new JObject();
                        obj_rules["listDesc"] = listdesc;
                        obj_rules["collectType"] = "collect";
                        JObject obj_cfg = new JObject();
                        obj_cfg["method"] = "ConfigService/setConfig";
                        JArray array_payload = new JArray();
                        array_payload.Add(obj_rules);
                        obj_cfg["params"] = array_payload;
                        obj["data"] = obj_cfg;
                        PublicConfig.LogTrace(string.Format("Document Configure Set Config{0}", obj.ToString()));
                        htmlElementDown.innerText = JsonConvert.SerializeObject(obj);
                        IDOMEvent event2 = PublicConfig.ghtmlDownDoc.createEvent("HTMLEvents");
                        event2.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                        bool bRetCfg = PublicConfig.ghtmlDownDoc.dispatchEvent(event2);
                        PublicConfig.LogTrace(string.Format("Document Configure Set Config Ret {0}", bRetCfg));
                        PublicConfig.LogTrace(string.Format("Document htmlElementDown.innerText 1 {0} \r\nstrText: {1}", htmlElementDown.innerText, JsonConvert.SerializeObject(obj)));
                    }
                    catch (Exception ex)
                    {
                        PublicConfig.ReturnErrorInfo(string.Format("配置页通信或者Rule格式有误，请确保IE8及以上内核且Rule格式正确!"), 5);
                        return;
                    }
                }
            }
        }
        public bool IEFromEngineToExecutorMessageByStart()
        {
            try
            {
                PublicConfig.gUpMutexInit.WaitOne();
                if (PublicConfig.ghtmlUpDoc != null)
                {
                    IHTMLElement htmlElementUp = PublicConfig.ghtmlUpDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                    if (htmlElementUp != null)
                    {                       
                        JObject obj = new JObject();
                        obj["from"] = "engine";
                        obj["to"] = "executor";
                        JObject obj_data = new JObject();
                        obj_data["method"] = "MainService/start";
                        obj_data["payload"] = new JArray();
                        obj["data"] = obj_data;
                        htmlElementUp.innerText = JsonConvert.SerializeObject(obj);
                        IDOMEvent event1 = PublicConfig.ghtmlUpDoc.createEvent("HTMLEvents");
                        event1.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                        bool bRet1 = PublicConfig.ghtmlUpDoc.dispatchEvent(event1);
                        PublicConfig.LogTrace(string.Format("Document Execute DispatchEvent {0}", bRet1));
                        PublicConfig.LogTrace(string.Format("Document  gthtmlElementUp.innerText 3 {0}\r\nstrText:{1}", htmlElementUp.innerText, JsonConvert.SerializeObject(obj)));
                        PublicConfig.gUpMutexInit.ReleaseMutex();
                        return true;
                    }
                }
                PublicConfig.gUpMutexInit.ReleaseMutex();
            }
            catch (Exception EX)
            {
                PublicConfig.gUpMutexInit.ReleaseMutex();
            }

            return false;
        }
        public void IEFromEngineToExecutorMessageByStartBrowser()
        {
            IHTMLDocument2 htmlDoc2;
            PublicConfig.GetDocument2(out htmlDoc2);
            if (htmlDoc2 != null)
            {
                HTMLDocument htmlDoc = htmlDoc2 as HTMLDocument;
                IHTMLElement htmlElementUp = htmlDoc.getElementById("SHI_ZAI_MESSAGE_ELEMENT_IN");
                if (htmlElementUp != null)
                {
                    JObject obj = new JObject();
                    obj["from"] = "engine";
                    obj["to"] = "executor";
                    JObject obj_data = new JObject();
                    obj_data["method"] = "MainService/startBrowserMode";
                    obj["data"] = obj_data;
                    htmlElementUp.innerText = JsonConvert.SerializeObject(obj);
                    IDOMEvent event1 = htmlDoc.createEvent("HTMLEvents");
                    event1.initEvent(PublicConfig.gstrShiZaiEventIn, false, false);
                    bool bRet = htmlDoc.dispatchEvent(event1);
                    PublicConfig.LogTrace(string.Format("Document Execute DispatchEvent {0}", bRet));
                    PublicConfig.LogTrace(string.Format("Document htmlElementUp.innerText 1 {0} \r\nstrText:{1}", htmlElementUp.innerText, JsonConvert.SerializeObject(obj)));
                }
            }
        }
    }
}
