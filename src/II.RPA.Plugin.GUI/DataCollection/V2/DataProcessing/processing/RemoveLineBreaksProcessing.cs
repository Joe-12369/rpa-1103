using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class RemoveLineBreaksProcessing : IProcessing
    {
        private readonly static Lazy<RemoveLineBreaksProcessing> _instance = new Lazy<RemoveLineBreaksProcessing>(() => new RemoveLineBreaksProcessing());
        public static IProcessing Instance => _instance.Value;

        public int Order => 99999;

        private RemoveLineBreaksProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            return (content ?? string.Empty).Replace("\r\n", " ").Replace("\n", " ");
        }
    }
}
