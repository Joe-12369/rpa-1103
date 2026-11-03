using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class RegularProcessing : IProcessing
    {
        private readonly static Lazy<RegularProcessing> _instance = new Lazy<RegularProcessing>(() => new RegularProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 100;

        private RegularProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            content = content ?? string.Empty;
            if (config.Parameters?.Length > 0)
            {
                string regexexpress = JObject.FromObject(config.Parameters[0]).Value<string>("val");
                return DataCollectRegexHelper.GenerateStringWithRegex(content, regexexpress);
            }
            return content;
        }
    }
}
