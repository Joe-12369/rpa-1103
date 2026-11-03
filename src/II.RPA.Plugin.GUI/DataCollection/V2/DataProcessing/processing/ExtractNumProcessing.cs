using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class ExtractNumProcessing : IProcessing
    {
        private readonly static Lazy<ExtractNumProcessing> _instance = new Lazy<ExtractNumProcessing>(() => new ExtractNumProcessing());
        public static IProcessing Instance => _instance.Value;
        private const string pattern = @"-?\d+\.?\d*";
        public int Order => 300;

        private ExtractNumProcessing()
        {

        }
        public string Processing(string content, Commons.Params.DataCollect.ProcessingConfig config)
        {
            MatchCollection matches = Regex.Matches(content, pattern);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Match m in matches)
                stringBuilder.Append(m.Value);
            return stringBuilder.ToString();
        }
    }
}
