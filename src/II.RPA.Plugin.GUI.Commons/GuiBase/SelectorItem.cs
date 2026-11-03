using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class SelectorItem:ObservableObject
    {
        public IGuiNode Element { get { return GetProperty<IGuiNode>(); } set { SetProperty(value); } }
        public bool Enabled { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool CanDisable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public List<SelectorItemProperty> Properties { get; set; }
        public SelectorItem() { Enabled = true; }
        public SelectorItem(JObject o)
        {
            this.OnBuild(o);
        }

        protected virtual void OnBuild(JObject o) 
        {
            this.Properties = new List<SelectorItemProperty>();
            foreach (var item in o)
            {
                this.Properties.Add(new SelectorItemProperty(item.Key, item.Value.ToString()));
            }
            foreach (var p in Properties)
            {
                p.Enabled = true;
                p.CanDisable = true;
            }
            this.Enabled = true;
            this.CanDisable = string.IsNullOrEmpty(Selector);
        }

        /// <summary>
        /// 获取选择器类型
        /// </summary>
        public string Selector
        {
            get
            {
                if (Properties == null) return null;
                var v = Properties.Where(x => x.Name == "selector").FirstOrDefault();
                if (v == null) return null;
                return v.Value;
            }
        }
        /// <summary>
        /// 返回单个选择器json表示
        /// </summary>
        public virtual JObject json
        {
            get 
            {
                var elements = new List<JContainer>();
                foreach(var j in Properties)
                {
                    if(j.Enabled==true)
                    {
                        elements.Add(j.JsonValue);
                    }
                }
                //JArray array = new JArray(elements);
                //JProperty jp = new JProperty("prop", array);
                //return new JObject(jp);
                return new JObject(elements);
            }
        }
        public string DisplayName
        {
            get {
                return json.ToString().Replace(Environment.NewLine, " ").Trim();
            }
        }
        public override string ToString()
        {
            return DisplayName;
        }
    }
}
