using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SharedZexploreData
    {
        public int Port;               // 端口号
        public int TabId;         // 启动的标签页ID      
        public int PId;//进程信息       
    }
}
