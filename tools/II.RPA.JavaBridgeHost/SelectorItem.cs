using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace II.RPA.JavaBridgeHost
{
    public class SelectorItem
    {
        public SelectorItem()
        {
            Enabled = true;
        }

        public SelectorItem(JObject o)
        {
            this.Properties = new List<JavaSelectorItemProperty>();
            foreach (var item in o)
            {
                this.Properties.Add(new JavaSelectorItemProperty(item.Key, item.Value.ToString()));
            }
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.CanDisable = true;
            }
            this.Enabled = true;
            this.CanDisable = true;
        }

        public bool CanDisable { get; set; }

        public string DisplayName
        {
            get
            {
                return json.ToString().Replace(Environment.NewLine, " ").Trim();
            }
        }

        public bool Enabled { get; set; }

        /// <summary>
        /// 返回单个选择器json表示
        /// </summary>
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

        public List<JavaSelectorItemProperty> Properties { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}