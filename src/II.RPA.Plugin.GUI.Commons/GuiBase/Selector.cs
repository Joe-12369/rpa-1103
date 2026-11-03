using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class Selector : List<SelectorItem>
    {
        public Selector() { }
        public Selector(string json, string version = null)
        {
            JArray a = JArray.Parse(json);
            foreach (JObject o in a)
            {
                var item = this.GenerateSelectorItem(o, version);
                if (item == null)
                    continue;
                //item.Enabled = true;
                Add(item);
            }
        }

        protected virtual SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return new SelectorItem(json);
        }

        public virtual IGuiNode[] GetElements(IGuiNode fromElement = null, int maxresults = 1) { return new IGuiNode[] { }; }
        public override string ToString()
        {
            var results = new List<JObject>();

            foreach (var j in this.ToList())
            {
                //if (j.Enabled) 
                    results.Add(j.json);
            }
            return new JArray(results).ToString();
        }
    }
}
