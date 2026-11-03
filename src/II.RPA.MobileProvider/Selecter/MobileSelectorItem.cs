
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Selecter
{
    public class MobileSelectorItem 
    {
        [Newtonsoft.Json.JsonIgnore()]
        public bool Enabled { get; set; }
        [Newtonsoft.Json.JsonIgnore()]
        public bool CanDisable { get; set; }
        [Newtonsoft.Json.JsonIgnore()]
        public List<MobileSelectorItemProperty> Properties { get; set; }
        public MobileSelectorItem() { Enabled = true; }
        public MobileSelectorItem(JObject o)
        {
            Properties = new List<MobileSelectorItemProperty>();
            foreach (var item in o)
            {
                Properties.Add(new MobileSelectorItemProperty(item.Key, item.Value.ToString()));
            }
            Properties.ForEach((p) => { p.Enabled = true; p.CanDisable = true; });
        }
        [Newtonsoft.Json.JsonIgnore()]
        public JObject json
        {
            get
            {
                var elements = new List<JProperty>();
                foreach (var j in Properties)
                {
                    if (j.Enabled == true)
                    {
                        elements.Add(j.json);
                    }
                }
                return new JObject(elements);
            }
        }
        public string DisplayName
        {
            get { return json.ToString().Replace(Environment.NewLine, "").Trim(); }
        }
        public override string ToString()
        {
            return DisplayName;
        }
    }
}
