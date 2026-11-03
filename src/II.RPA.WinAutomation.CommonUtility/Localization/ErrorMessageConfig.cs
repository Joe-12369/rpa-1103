using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class ErrorMessageConfig
    {
        private static ILogger logger = Logger.OfContext();
        private static Dictionary<string, string> _currentLanguageDict { get; set; } = new Dictionary<string, string>();

        static ErrorMessageConfig()
        {
            _currentLanguageDict = ConfResourceLoader.LoadResourceDictionary(typeof(eErrorMessageType).Name);
        }

        public static string GetMessageByCode(string code)
        {
            string result = string.Empty;
            if (!_currentLanguageDict.TryGetValue(code, out result))
            {
                logger.LogWarning($"error message not found,code:{code}");
            }
            return result;
        }

        public static string GetMessage(eErrorMessageType errorMsgType)
        {
            string errorMessage = string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = GetMessageByCode($"{(int)errorMsgType}".PadLeft(4, '0'));
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = GetMessageByCode($"{(int)errorMsgType}".PadLeft(3, '0'));
                }
            }

            return errorMessage;
        }
    }

    internal class ErrorMessageAttribute : Attribute
    {
        public string Discription { get; set; }
        public ErrorMessageAttribute(string discription = "")
        {
            Discription = discription;
        }
    }
}
