using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Dialog.Helpers
{
    internal class IgnoreCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}
