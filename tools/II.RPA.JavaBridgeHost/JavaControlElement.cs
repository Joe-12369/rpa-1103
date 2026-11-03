using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsAccessBridgeInterop;

namespace II.RPA.JavaBridgeHost
{
    class JavaControlElement
    {
        AccessibleContextNode node;
        int depth;
        public JavaControlElement(AccessibleContextNode node,int depth)
        {
            this.node = node;
            this.depth = depth;
        }
        public int Depth
        {
            get {
                return depth;
            }
        }
        public AccessibleContextInfo AccInfo
        {
            get {
                return node.GetInfo();
            }
        }
        public string Name
        {
            get
            {
                return node.GetInfo().name;
            }
        }
    }
}
