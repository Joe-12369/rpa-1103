using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace II.RPA.JavaBridgeHost
{
    public abstract class Selector : List<SelectorItem>
    {
        public Selector()
        {
        }

        public Selector(string json, string version)
        {
            JArray a = JArray.Parse(json);
            foreach (JObject o in a)
            {
                var item = this.GenerateSelectorItem(o, version);
                if (item == null)
                    continue;
                item.Enabled = true;
                Add(item);
            }
        }

        public virtual JavaNode[] GetElements(JavaNode fromElement = null, int maxresults = 1)
        {
            return new JavaNode[] { };
        }

        public override string ToString()
        {
            var results = new List<JObject>();
            foreach (var j in this.ToList())
            {
                if (j.Enabled) results.Add(j.json);
            }
            return new JArray(results).ToString();
        }

        protected virtual SelectorItem GenerateSelectorItem(JObject json, string version)
        {
            return new SelectorItem(json);
        }
    }
}