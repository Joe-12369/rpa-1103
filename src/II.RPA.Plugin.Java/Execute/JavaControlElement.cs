using WindowsAccessBridgeInterop;

namespace II.RPA.Plugin.Java.Execute
{
    internal class JavaControlElement
    {
        private int depth;
        private AccessibleContextNode node;

        public JavaControlElement(AccessibleContextNode node, int depth)
        {
            this.node = node;
            this.depth = depth;
        }

        public AccessibleContextNode AccNode
        {
            get
            {
                return node;
            }
        }

        public int Depth
        {
            get
            {
                return depth;
            }
        }
        public string Name
        {
            get
            {
                return node.GetInfo().name;
            }
        }
        public AccessibleContextInfo AccInfo
        {
            get
            {
                return node.GetInfo();
            }
        }
    }
}