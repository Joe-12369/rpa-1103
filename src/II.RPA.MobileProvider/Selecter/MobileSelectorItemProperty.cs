
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Selecter
{
    public class MobileSelectorItemProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Enabled { get; set; }
        public bool CanDisable { get; set; }

        public MobileSelectorItemProperty(string name, string value)
        {
            Name = name;
            Value = value;
            Enabled = (name != "ControlType");
            CanDisable = true;
        }
        public JProperty json
        {
            get
            {
                if (!Enabled) return null;
                return new JProperty(Name, Value);
            }
        }
        public override string ToString()
        {
            return new JProperty(Name, Value).ToString();
        }
    }
}
