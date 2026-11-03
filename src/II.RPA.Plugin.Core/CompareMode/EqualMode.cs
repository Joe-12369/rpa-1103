using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.CompareMode
{
    public class EqualMode : ICompareMode<string>
    {
        public bool Compare(string fromObj, string toObj)
        {
            if (string.IsNullOrEmpty(fromObj) && string.IsNullOrEmpty(toObj))
            {
                return true;
            }
            if (string.IsNullOrEmpty(fromObj) || string.IsNullOrEmpty(toObj))
            {
                return false;
            }
            return fromObj.Equals(toObj);
        }
    }
}
