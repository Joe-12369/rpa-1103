using II.RPA.Core.Runtime;
using II.RPA.Core.Common;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("ProcessCreator")]
    internal class ProcessCreatorProvider : BaseActionProvider
    {
        private readonly JObject m_ReqValObject = new JObject
        {
            { "isCVCapture","False"},
            { "InspectStatus",0},
        };
        public ProcessCreatorProvider(ILogger aLogger) : base(aLogger)
        {
            
        }
        public ProcessCreatorProvider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = false;
        }
        public override string ResolveReqVal(string aReqVal)
        {
            JObject reqValJObject = JObject.Parse(base.ResolveReqVal(aReqVal));
            int cvflag = 0;
            var elementTypes = reqValJObject["elementTypes"];
            if (elementTypes is JArray elementTypesArray)
            {
                var cv = elementTypesArray.FirstOrDefault(e => e.ToString() == "cv");
                if (cv != null)
                {
                    cvflag = 1;
                }
            }
            m_ReqValObject.Add("IsCvSupport", cvflag);
            m_ReqValObject.Add("CaptureMode", reqValJObject.Value<string>("type"));
            m_ReqValObject.Add("cv_server_url", cvPickServer);
            m_ReqValObject.Add("filepath", m_APicturePath);
            m_ReqValObject.Add("action", reqValJObject.Value<int>("action"));
            m_ReqValObject.Add("packagepath", m_PackagePath);
            m_ReqValObject.Add("componentList", reqValJObject["componentList"]);
            m_ReqValObject.Add("factoryWindowHandle", reqValJObject.Value<string>("factoryWindowHandle"));
            m_ReqValObject.Add("clientType", reqValJObject.Value<string>("clientType"));
            m_ReqValObject.Add("addition", reqValJObject.Value<string>("addition"));

            if (reqValJObject.Value<int>("action") == 0)
            {
                Global.IsProcessCreatorRunning = true;
            }
            else if (reqValJObject.Value<int>("action") == 1)
            {
                Global.IsProcessCreatorRunning = false;
            }

            return m_ReqValObject.ToString();
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.ProcessCreator"
            };
        }
    }
}
