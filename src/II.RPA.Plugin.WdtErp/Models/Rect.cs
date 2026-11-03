using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WdtErp
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct RECT
    {

        /// LONG->int
        public int left;

        /// LONG->int
        public int top;

        /// LONG->int
        public int right;

        /// LONG->int
        public int bottom;
    }
}
