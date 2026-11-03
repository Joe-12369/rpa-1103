using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows.CompareRule
{
    public class RuleLessThanRule : ICompareRule<decimal>
    {
        public bool Compare(decimal fromObj, decimal toObj)
        {
            return fromObj < toObj;
        }
    }
}
