using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginInterfere
{
    public class ElementPlugin
    {
        private readonly JObject PluginParamJO;
        public bool m_HasFound;
        public ElementPlugin(string PluginParamJson,
            string PluginName,
            string PluginAction)
        {
            PluginParamJO = JObject.Parse(PluginParamJson);
        }

        public virtual bool IsCurrentPlugin()
        {
            return PluginParamJO.ContainsKey("selector");
        }
        public void SetFindResult(bool anHasFound)
        {
            m_HasFound = anHasFound;
        }
        public bool IsSmartControl()
        {
            string selectroStr = PluginParamJO["selector"].ToString();
            var selectorJO = JObject.Parse(selectroStr);
            var issmartcontrol = selectorJO["commonProps"].Value<bool>("issmartcontrol");
            return issmartcontrol;
        }
        public virtual bool NeedHandleAfterTimeOut(bool enableProcessRepaireControl = false)
        {
            if (IsCurrentPlugin())
            {
                if (!m_HasFound && (IsSmartControl() || enableProcessRepaireControl))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
