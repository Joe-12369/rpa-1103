using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class PrefixProcessing : IProcessing
    {
        private readonly static Lazy<PrefixProcessing> _instance = new Lazy<PrefixProcessing>(() => new PrefixProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 400;

        private PrefixProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            if (config.Parameters?.Length == 0)
            {
                return content ?? string.Empty;
            }
            string prefix = JObject.FromObject(config.Parameters[0]).Value<string>("val");
            return $"{prefix ?? string.Empty}{content ?? string.Empty}";
        }
    }
}
