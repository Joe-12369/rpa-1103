using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.CompareRule
{
    public class RuleNotContains : ICompareRule<string>
    {
        public bool Compare(string fromObj, string toObj)
        {
            if (fromObj == null && toObj == null)
            {
                return false;
            }

            if (fromObj == string.Empty && toObj == string.Empty)
            {
                return false;
            }

            if (fromObj == null && toObj != null)
            {
                return true;
            }

            if (fromObj != null && toObj == null)
            {
                return true;
            }

            return !fromObj.Contains(toObj);
        }
    }
}
