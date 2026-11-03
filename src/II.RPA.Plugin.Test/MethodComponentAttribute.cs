using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Test
{
    public class MethodComponentAttribute : Attribute
    {
        public string Name { get; set; }
        public MethodComponentAttribute(string name)
        {
            this.Name = name;
        }
    }
}
