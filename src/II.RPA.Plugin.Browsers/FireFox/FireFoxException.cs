using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.FireFox
{
    class FireFoxException : RPAException
    {
        public FireFoxException()
            : base()
        {
        }

        internal FireFoxException(int errorCode, eComponentType componentType)
            : base(eGuiType.FireFox, errorCode, componentType, null)
        {

        }
        internal FireFoxException(eErrorMessageType errorCode)
        : base(eGuiType.FireFox, errorCode, eComponentType.None, null)
        {

        }
        internal FireFoxException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.FireFox, errorCode, componentType, null)
        {

        }
        internal FireFoxException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.FireFox, errorCode, eComponentType.None, e)
        {

        }
        internal FireFoxException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Chrome, errorCode, componentType, e)
        {

        }
        internal FireFoxException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.FireFox, errorCode, componentType, e)
        {

        }
    }
}
