using System.Collections.Generic;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class CommonMessageConfig
    {
        private static Dictionary<string, string> _currentLanguageDict = null;
        static CommonMessageConfig()
        {
            _currentLanguageDict = ConfResourceLoader.LoadResourceDictionary(typeof(eCommonMessageType).Name);
        }

        public static string GetMessageByCode(string code)
        {
            if (_currentLanguageDict != null && _currentLanguageDict.TryGetValue(code, out string result))
            {
                return result;
            }
            return string.Empty;

        }

        public static string GetMessage(eCommonMessageType CommonMsgType)
        {
            int code = (int)CommonMsgType;
            string msg = GetMessageByCode(code.ToString("0000"));
            if (string.IsNullOrEmpty(msg))
            {
                msg = GetMessageByCode(code.ToString("000"));
            }
            return msg;
        }
    }

}
