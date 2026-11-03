using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    /// <summary>
    /// verifySelector action 处理逻辑过于复杂、暂时由engine实现入参出参处理。
    /// 前端继续调用engine、engine调用grpc
    /// </summary>
    [ClientActionName("verifySelector")]
    internal class UI_Verify_Selector_Provider : BaseActionProvider
    {
        private JToken uiSelector;
        public UI_Verify_Selector_Provider(ILogger aLogger) : base(aLogger)
        {
        }
        public UI_Verify_Selector_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            var valJO = JObject.Parse(aReqVal);
            uiSelector = valJO["selector"];
            PluginRunAction action = new PluginRunAction();
            if (uiSelector["commonProps"] != null)
            {
                m_Logger.LogDebug("新版Selector");
                if (valJO.ContainsKey("variableMap"))
                {
                    var variableMap = valJO["variableMap"] as JObject;
                    List<string> vars = new List<string>();
                    foreach (var variable in variableMap)
                    {
                        vars.Add($"{variable.Key}={variable.Value}");
                    }
                    var data = string.Join(",", vars);
                }
            }
            else
            {
                m_Logger.LogDebug("旧版Selector");
            }
            return action;
        }
    }
}
