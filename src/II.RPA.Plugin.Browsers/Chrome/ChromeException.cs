using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.Chrome
{
    class ChromeException : RPAException
    {
        public ChromeException()
            : base()
        {
        }
       
        internal ChromeException(int errorCode, eComponentType componentType)
            : base(eGuiType.Chrome, errorCode, componentType, null)
        {

        }
        internal ChromeException(eErrorMessageType errorCode)
        : base(eGuiType.Chrome, errorCode, eComponentType.None, null)
        {

        }
        internal ChromeException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Chrome, errorCode, componentType, null)
        {

        }
        internal ChromeException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Chrome, errorCode, eComponentType.None, e)
        {

        } 
        internal ChromeException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Chrome, errorCode, componentType, e)
        {

        }
        internal ChromeException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Chrome, errorCode, componentType, e)
        {

        }
    }
}
