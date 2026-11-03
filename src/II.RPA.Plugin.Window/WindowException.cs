using System;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.Window
{
    class WindowException : RPAException
    {
        public WindowException()
         : base()
        {
        }
        internal WindowException(int errorCode, eComponentType componentType)
            : base(eGuiType.Window, errorCode, componentType, null)
        {

        }
        internal WindowException(eErrorMessageType errorCode)
        : base(eGuiType.Window, errorCode, eComponentType.None, null)
        {

        }
        internal WindowException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Window, errorCode, componentType, null)
        {

        }
        internal WindowException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.Window, errorCode, eComponentType.None, e)
        {

        }
        internal WindowException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Window, errorCode, componentType, e)
        {

        }
        internal WindowException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Window, errorCode, componentType, e)
        {

        }
    }
}
