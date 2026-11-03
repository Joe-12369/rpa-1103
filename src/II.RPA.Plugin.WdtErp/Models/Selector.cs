using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WdtErp.Models
{
    public class WdtErpSelector
    {
        public Node[] Nodes { get; set; }
    }

    public class Node
    {
        public string name { get; set; }
        public string type { get; set; }
        public Prop[] props { get; set; }
        public string accurate { get; set; } = "true";
    }

    public class Prop
    {
        public string name { get; set; }
        public string value { get; set; }
        public string pattern { get; set; }
        public string accurate { get; set; }
    }

}
