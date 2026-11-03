using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class ReplaceProcessing : IProcessing
    {
        private readonly static Lazy<ReplaceProcessing> _instance = new Lazy<ReplaceProcessing>(() => new ReplaceProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 200;

        private ReplaceProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            content = content ?? string.Empty;
            if (config.Parameters?.Length > 0)
            {
                foreach (var param in config.Parameters)
                {
                    var replaceParam = JObject.FromObject(param);
                    content = content.Replace(replaceParam.Value<string>("text"), replaceParam.Value<string>("replaceText"));
                }
            }
            return content;
        }
    }
}
