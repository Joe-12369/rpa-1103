using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{
    public class CollectAttribute : Attribute
    {
        public string AttributeName { get; set; } = string.Empty;
        public CollectAttribute(string attributeName = "")
        {
            AttributeName = attributeName;
        }
    }
}
