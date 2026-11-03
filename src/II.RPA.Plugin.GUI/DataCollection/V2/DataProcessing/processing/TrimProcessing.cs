using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class TrimProcessing : IProcessing
    {
        private readonly static Lazy<TrimProcessing> _instance = new Lazy<TrimProcessing>(() => new TrimProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 100;

        private TrimProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            return (content ?? string.Empty).Trim();
        }
    }
}
