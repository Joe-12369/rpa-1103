using II.RPA.WebBrowser.Common.Basic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using II.RPA.Plugin.IE.V2.Automation;
using System.Runtime.InteropServices;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.Plugin.IE.V2.Automation.Core;

namespace II.RPA.Plugin.IE.V2
{
    public class IEWebDataProvider : WebDataProvider
    {
        private IEAutomation IEAutomation => IEAutomation.Instance;
        private Dictionary<string, MethodInfo> IEAutomationMethodInfoDic { get; set; }
        private static ILogger logger = Logger.OfContext();


        public IEWebDataProvider(ConnectionConfig connConfig) : base(connConfig)
        {
            InitCommandActionDic();
        }

        private void InitCommandActionDic()
        {
            IEAutomationMethodInfoDic = new Dictionary<string, MethodInfo>();
            var methods = this.IEAutomation.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var md in methods)
            {
                var attr = md.GetCustomAttribute<DisplayNameAttribute>();
                if (attr != null)
                {
                    IEAutomationMethodInfoDic[attr.DisplayName] = md;
                }
            }
        }
        private JObject ParamsResolver(object values)
        {
            var param = JObject.Parse(values.ToString());
            return param;
        }
        public override JToken Send(string command, JObject @params, int timeout = 10000, bool ignoreConnection = false)
        {
            var paramsModel = ParamsResolver(@params);
            if (IEAutomationMethodInfoDic.ContainsKey(command))
            {
                try
                {
                    var result = ThreadManager.BackToUIThread(() =>
                    {
                        return IEAutomationMethodInfoDic[command].Invoke(this.IEAutomation, new object[] { paramsModel });
                    });
                    if (!string.IsNullOrEmpty(result?.ToString()))
                    {
                        if (result is string)
                        {
                            return result?.ToString();
                        }
                        else
                        {
                            if (TryParse(result.ToString(), out JToken resultToken))
                                return resultToken;
                            else
                                return (JToken)result;
                        }
                    }
                    else
                    {
                        return result?.ToString();
                    }
                }
                catch (COMException comex)
                {
                    logger.LogWarning(comex.Message);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        public bool TryParse(string value, out JToken resultToken)
        {
            try
            {
                resultToken = JToken.Parse(value);
                return true;
            }
            catch (Exception)
            {
                resultToken = null;
            }
            return false;
        }
        internal T Send<T>(string command, object @params, int timeout = 10000)
        {
            var paramsModel = ParamsResolver(@params);
            if (IEAutomationMethodInfoDic.ContainsKey(command))
            {
                var result = IEAutomationMethodInfoDic[command].Invoke(this.IEAutomation, new object[] { paramsModel });
                return (T)result;
            }
            return (T)default;
        }

        internal void ClearIHtmlObject()
        {
            this.Send("tabs.ClearIHtmlObject", new JObject());
        }
    }
}
