using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal class DateAfterFilter : DataFilter<DateAfterFilter>
    {
        public override bool Execute(string content, FilterConfig config)
        {
            if (!(config.Parameters?.Count > 0) || string.IsNullOrEmpty(content))
            {
                return false;
            }
            try
            {
                DateTime dateTime = Convert.ToDateTime(content);
                DateTime compareDateTime = Convert.ToDateTime(config.Parameters[0]);
                return dateTime > compareDateTime;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
