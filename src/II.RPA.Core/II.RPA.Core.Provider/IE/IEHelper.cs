using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.IE
{
    public static class IEHelper
    {
        [DllImport(@"IE\Element.dll", EntryPoint = "DoFindElement", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool DoFindElement(string json, bool flag);
    }
}
