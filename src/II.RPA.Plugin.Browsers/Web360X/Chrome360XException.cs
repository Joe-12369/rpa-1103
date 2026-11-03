using II.RPA.WinAutomation.CommonUtility;
using System;

namespace II.RPA.Plugin.Browsers.Web360X
{
    class Chrome360XException : RPAException
    {
        public Chrome360XException()
            : base()
        {
        }
       
        internal Chrome360XException(int errorCode, eComponentType componentType)
            : base(eGuiType.Web360se, errorCode, componentType, null)
        {

        }
        internal Chrome360XException(eErrorMessageType errorCode)
        : base(eGuiType.Web360se, errorCode, eComponentType.None, null)
        {

        }
        internal Chrome360XException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Web360se, errorCode, componentType, null)
        {

        }
        internal Chrome360XException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Web360se, errorCode, eComponentType.None, e)
        {

        } 
        internal Chrome360XException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Web360se, errorCode, componentType, e)
        {

        }
        internal Chrome360XException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Web360se, errorCode, componentType, e)
        {

        }
    }
}
