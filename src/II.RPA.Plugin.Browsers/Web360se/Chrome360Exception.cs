using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.Web360se
{
    class Chrome360Exception : RPAException
    {
        public Chrome360Exception()
            : base()
        {
        }
       
        internal Chrome360Exception(int errorCode, eComponentType componentType)
            : base(eGuiType.Web360se, errorCode, componentType, null)
        {

        }
        internal Chrome360Exception(eErrorMessageType errorCode)
        : base(eGuiType.Web360se, errorCode, eComponentType.None, null)
        {

        }
        internal Chrome360Exception(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Web360se, errorCode, componentType, null)
        {

        }
        internal Chrome360Exception(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Web360se, errorCode, eComponentType.None, e)
        {

        } 
        internal Chrome360Exception(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Web360se, errorCode, componentType, e)
        {

        }
        internal Chrome360Exception(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Web360se, errorCode, componentType, e)
        {

        }
    }
}
