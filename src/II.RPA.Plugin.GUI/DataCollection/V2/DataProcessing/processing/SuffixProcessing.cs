using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class SuffixProcessing : IProcessing
    {
        private readonly static Lazy<SuffixProcessing> _instance = new Lazy<SuffixProcessing>(() => new SuffixProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 500;

        private SuffixProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            if (config.Parameters?.Length == 0)
            {
                return content ?? string.Empty;
            }
            string suffix = JObject.FromObject(config.Parameters[0]).Value<string>("val");
            return $"{content ?? string.Empty}{suffix ?? string.Empty}";

        }
    }
}
