using II.RPA.WinAutomation.CommonUtility.Utils;
using System.Collections.Generic;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class ComponentMessageConfig : SingleInstance<ComponentMessageConfig>
    {
        private Dictionary<string, string> _currentLanguageDict = null;
        public ComponentMessageConfig()
        {
            _currentLanguageDict = ConfResourceLoader.LoadResourceDictionary(typeof(eComponentType).Name);
        }

        public string GetComponentTextByCode(string code)
        {
            if (_currentLanguageDict != null && _currentLanguageDict.TryGetValue(code, out string result))
            {
                return result;
            }
            return string.Empty;
        }

        public string GetComponentTextByCode(eComponentType _MessageType)
        {
            string code = ((int)_MessageType).ToString("000");
            if (_currentLanguageDict != null && _currentLanguageDict.TryGetValue(code, out string result))
            {
                return result;
            }
            return _MessageType.ToString();
        }

    }
}
