using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost.CompareRule
{
    public class RuleSmallerRule : ICompareRule<decimal>
    {
        public bool Compare(decimal fromObj, decimal toObj)
        {
            return fromObj < toObj;
        }
    }
}
