using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal class RegularTestFilter : DataFilter<RegularTestFilter>
    {
        public override bool Execute(string content, FilterConfig config)
        {
            if (!(config.Parameters?.Count > 0))
            {
                return false;
            }
            Regex regex = new Regex(config.Parameters[0]);
            return regex.IsMatch(content);
        }
    }
}
