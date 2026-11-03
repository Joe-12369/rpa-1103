using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.Chromium
{
    class ChromiumException : RPAException
    {
        public ChromiumException()
            : base()
        {
        }
       
        internal ChromiumException(int errorCode, eComponentType componentType)
            : base(eGuiType.Chromium, errorCode, componentType, null)
        {

        }
        internal ChromiumException(eErrorMessageType errorCode)
        : base(eGuiType.Chromium, errorCode, eComponentType.None, null)
        {

        }
        internal ChromiumException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Chromium, errorCode, componentType, null)
        {

        }
        internal ChromiumException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Chromium, errorCode, eComponentType.None, e)
        {

        } 
        internal ChromiumException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Chromium, errorCode, componentType, e)
        {

        }
        internal ChromiumException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Chromium, errorCode, componentType, e)
        {

        }
    }
}
