using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.CompareRule
{
    public class RuleRuleRegular : ICompareRule<string>
    {
        public bool Compare(string fromObj, string toObj)
        {
            if (fromObj == null && toObj == null)
            {
                return true;
            }

            if (fromObj == string.Empty && toObj == string.Empty)
            {
                return true;
            }

            if (fromObj == null || toObj == null)
            {
                return false;
            }

            return Regex.IsMatch(fromObj, toObj);
        }
    }
}
