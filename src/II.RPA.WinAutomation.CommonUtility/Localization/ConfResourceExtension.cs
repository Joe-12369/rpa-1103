using System.Linq;
using System.Reflection;

namespace II.RPA.WinAutomation.CommonUtility
{
    public static class CommonMessageConfigExtension
    {
        public static string GetMessage(this eCommonMessageType commonMsgType)
        {
            string strMessage = string.Empty;
            strMessage = CommonMessageConfig.GetMessage(commonMsgType);
            return strMessage;
        }        
        
     }
    public static class ErrorMessageConfigExtensions
    {
        public static string GetMessage(this eErrorMessageType errorMsgType)
        {
            string errorMessage = string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = ErrorMessageConfig.GetMessageByCode(((int)errorMsgType).ToString("000"));
            }
            if (string.IsNullOrEmpty(errorMessage))
            {
                string name = errorMsgType.ToString();
                FieldInfo fieldInfo = errorMsgType.GetType().GetField(name);
                ErrorMessageAttribute errorMessageAttr = fieldInfo.GetCustomAttributes(typeof(ErrorMessageAttribute), false).FirstOrDefault() as ErrorMessageAttribute;
                if (errorMessageAttr != null)
                {
                    errorMessage = errorMessageAttr.Discription;
                }
            }
            return errorMessage;
        }
    }
}
