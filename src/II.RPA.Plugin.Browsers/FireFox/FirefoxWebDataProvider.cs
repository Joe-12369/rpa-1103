using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.FireFox
{
    public class FirefoxWebDataProvider : WebDataProvider
    {
        public FirefoxWebDataProvider(ConnectionConfig connectionConfig) : base(connectionConfig)
        {
        }

        #region 网页监听
        public override void EndInspectNetwork(ParamModel paramModel)
        {
            this.Send("webRequest.endListen", new JObject { });//firefox 不需要参数
        }
        public override void EnableNetwork(ParamModel paramModel)
        {
            this.Send("webRequest.startListen", new JObject { { "tabId", paramModel.TabId } });
        }

        public override void AddNetworkCache(ParamModel paramModel)
        {
            this.Send("webRequest.addCache", new JObject { { "tabId", paramModel.TabId } });
        }
        public override void StartInspectNetwork(ParamModel paramModel)
        {
            this.AddNetworkCache(paramModel);
            this.EnableNetwork(paramModel);
        }

        public override List<JObject> GetInspectNetwork(ParamInspectNetworkModel paramModel)
        {
            var isReady = this.Send("webRequest.checkCache", new JObject { { "tabId", paramModel.TabId } }).Value<bool>();
            if (!isReady)
            {
                throw new FireFoxException(eErrorMessageType.Error_Inspect_Not_Ready);
            }
            var httpReceived = this.Send("webRequest.getCache", new JObject { { "tabId", paramModel.TabId } }).ToObject<List<JObject>>();
            if (paramModel.type != "All")
            {
                httpReceived = httpReceived.Where(r => r.Value<string>("type") == paramModel.type).ToList();
            }
            if (!string.IsNullOrWhiteSpace(paramModel.url))
            {
                if (paramModel.isRegex)
                {
                    var regex = new Regex(paramModel.url, RegexOptions.IgnoreCase);
                    httpReceived = httpReceived.Where(r => regex.IsMatch(r.Value<string>("url"))).ToList();
                }
                else
                {
                    httpReceived = httpReceived.Where(r => r.Value<string>("url").Contains(paramModel.url)).ToList();
                }
            }
            httpReceived.ForEach(r =>
            {
                var headers = r["headers"].ToObject<List<JObject>>();
                var contentType = headers.FirstOrDefault(h => h.Value<string>("name").ToLower() == "content-type")?.Value<string>("value");
                if (string.IsNullOrWhiteSpace(contentType))
                {
                    return;
                }
                var bodyContent = this.Send("webRequest.getBody", new JObject { { "tabId", paramModel.TabId }, { "requestId", r.Value<string>("requestId") }, { "type", r.Value<string>("type") }, { "contentType", contentType } });
                if (string.IsNullOrWhiteSpace(bodyContent.Value<string>("body")))
                {
                    return;
                }
                r.Add("body", bodyContent.Value<string>("body"));
                r.Add("base64Encoded", bodyContent.Value<bool>("base64Encoded"));
            });
            return httpReceived;
        }
        #endregion
    }
}
