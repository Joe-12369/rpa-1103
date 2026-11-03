using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal class UnequalFilter : DataFilter<UnequalFilter>
    {
        public override bool Execute(string content, FilterConfig config)
        {
            if (!(config.Parameters?.Count > 0))
            {
                return false;
            }
            return content != config.Parameters[0];
        }
    }
}
