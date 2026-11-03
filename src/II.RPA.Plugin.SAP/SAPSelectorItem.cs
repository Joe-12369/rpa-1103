using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.Plugin.SAP
{
    public class SAPSelectorItem : SelectorItem
    {
        public string ElementName { get; set; }
        public SAPSelectorItem() { }
        public SAPSelectorItem(SelectorItem item)
        {
            SetBackingFieldValues(item._backingFieldValues);
            Properties = item.Properties;
        }
        public SAPSelectorItem(string id, string ext = null)
        {
            Properties = new List<SelectorItemProperty>();
            Properties.Add(new SapSelectorItemProperty("sapId", id));


            if (!string.IsNullOrEmpty(ext))
            {
                string relExtPath = ext;
                string relExtId=string.Empty;
                if(ext.Contains("extId"))
                {
                    int index = ext.IndexOf("extId");
                    relExtPath = ext.Substring(0, index - 1);
                    relExtId= ext.Substring(index);
                    string[] extIds = relExtId.Split(':');
                    if(extIds.Count()==2)
                    {
                        relExtId = extIds[1];
                    }
                }
                Properties.Add(new SapSelectorItemProperty("ext", relExtPath));
                if(relExtId!=string.Empty)
                {
                    Properties.Add(new SapSelectorItemProperty("extId", relExtId));
                }
            }
            this.ElementName = "Sap";
        }
        public SAPSelectorItem(AutomationElement element)
        {
            Properties = new List<SelectorItemProperty>();
            if (element.Properties.ProcessId.IsSupported)   //如果是顶层进程元素
            {
                //var info = element.GetProcessInfo();
                //if (!string.IsNullOrEmpty(info.Filename))
                //{
                //    if (info.Filename.ToLower().Contains("system32\\conhost.exe"))
                //    {
                //        info.Filename = "%windir%\\system32\\cmd.exe";
                //    }
                //    Properties.Add(new SapSelectorItemProperty("fileName", info.Filename));
                //}
                //Properties.Add(new SapSelectorItemProperty("processName", info.ProcessName));
                //Properties.Add(new SelectorItemProperty("arguments", info.Arguments));
                //Properties.Add(new SapSelectorItemProperty("selector", "SAP"));
                //Properties.Add(new SapSelectorItemProperty("className", element.ClassName));
                //Properties.Add(new SapSelectorItemProperty("title", element.Name));
                Properties.Add(new SapSelectorItemProperty("class", element.ClassName));
                Properties.Add(new SapSelectorItemProperty("name", element.Name));
            }
            this.ElementName = element.Properties.ControlType.IsSupported ? element.Properties.ControlType.Value.ToString() : "custom";
        }
        public override JObject json
        {
            get
            {
                SelectorProps selectorProp = new SelectorProps()
                {
                    Name = this.ElementName,
                    //Type = "Sap",
                    Props = this.Properties.OfType<SapSelectorItemProperty>().Where(x => x.Enabled).Select(x => x.Convert2Props()).ToList()
                };

                return JObject.FromObject(selectorProp);
            }
        }

    }
}
