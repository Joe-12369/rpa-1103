using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing
{
    internal class ProcessingFactory
    {
        public ProcessingFactory()
        {

        }
        internal static string Processing(string content, List<Commons.Params.DataCollect.ProcessingConfig> configs)
        {
            IProcessing processing = null;
            Dictionary<Commons.Params.DataCollect.ProcessingConfig, IProcessing> processingDic = new Dictionary<Commons.Params.DataCollect.ProcessingConfig, IProcessing>();
            foreach (var config in configs)
            {
                if (!config.Enabled)
                {
                    continue;
                }
                switch (config.ProcessType)
                {
                    case ProcessingType.Prefix:
                        processing = PrefixProcessing.Instance;
                        break;
                    case ProcessingType.Suffix:
                        processing = SuffixProcessing.Instance;
                        break;
                    case ProcessingType.Trim:
                        processing = TrimProcessing.Instance;
                        break;
                    case ProcessingType.Replace:
                        processing = ReplaceProcessing.Instance;
                        break;
                    case ProcessingType.ExtractNum:
                        processing = ExtractNumProcessing.Instance;
                        break;
                    case ProcessingType.FormatTime:
                        processing = FormatTimeProcessing.Instance;
                        break;
                    case ProcessingType.RemoveLineBreaks:
                        processing = RemoveLineBreaksProcessing.Instance;
                        break;
                    case ProcessingType.Regular:
                        processing = RegularProcessing.Instance;
                        break;
                    case ProcessingType.None:
                    default:
                        processing = null;
                        break;
                }
                if (processing != null)
                {
                    processingDic.Add(config, processing);
                }
            }
            processingDic.OrderBy(p => p.Value.Order).ForEach(p =>
            {
                content = p.Value.Processing(content, p.Key);
            });
            return content;
        }
    }
}
