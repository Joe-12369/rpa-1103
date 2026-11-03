using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2.DataFilter
{
    internal class DateBetweenFilter : DataFilter<DateBetweenFilter>
    {
        public override bool Execute(string content, FilterConfig config)
        {
            if ((config.Parameters?.Count != 1) || string.IsNullOrEmpty(content) || config.Parameters[0].IndexOf(",") == -1)
            {
                return false;
            }
            try
            {
                DateTime dateTime = Convert.ToDateTime(content);
                var datatimes = config.Parameters[0].Split(',');
                DateTime compareDateTime1 = Convert.ToDateTime(datatimes[0]);
                DateTime compareDateTime2 = Convert.ToDateTime(datatimes[1]);
                return dateTime >= compareDateTime1 && dateTime <= compareDateTime2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
