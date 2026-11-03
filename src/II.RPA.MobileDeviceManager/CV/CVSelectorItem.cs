using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.MobileDeviceManager.CV
{
    public class CVSelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        public List<SelectorItemProperty> Properties { get; set; }
        public CVSelectorItem()
        {
            Properties = new List<SelectorItemProperty>();
        }
        public CVSelectorItem(JObject json)
        {
            Properties = new List<SelectorItemProperty>();
            foreach (var item in json)
            {
                Properties.Add(new SelectorItemProperty(item.Key, item.Value.ToString()));
            }
        }
        public CVSelectorItem(string elementObjStr, string anchorObjStr)
        {
            try
            {
                Properties = new List<SelectorItemProperty>
                {
                    new SelectorItemProperty("ElementObj", elementObjStr),
                    new SelectorItemProperty("AnchorObj", anchorObjStr)
                };
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "CV Exception");
            }
        }
        public JObject json
        {
            get
            {
                var elements = new List<JProperty>();
                foreach (var j in Properties)
                {
                    elements.Add(j.json);
                }
                return new JObject(elements);
            }
        }
    }

    public class SelectorItemProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public SelectorItemProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public JProperty json => new JProperty(Name, Value);
    }
}

