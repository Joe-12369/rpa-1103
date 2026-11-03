using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Browsers.SuperBrowser
{
    public class SuperBrowserException:RPAException
    {
        public SuperBrowserException()
            : base()
        {
        }

        internal SuperBrowserException(int errorCode, eComponentType componentType)
            : base(eGuiType.SuperBrowser, errorCode, componentType, null)
        {

        }
        internal SuperBrowserException(eErrorMessageType errorCode)
        : base(eGuiType.SuperBrowser, errorCode, eComponentType.None, null)
        {

        }
        internal SuperBrowserException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.SuperBrowser, errorCode, componentType, null)
        {

        }
        internal SuperBrowserException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.SuperBrowser, errorCode, eComponentType.None, e)
        {

        }
        internal SuperBrowserException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.SuperBrowser, errorCode, componentType, e)
        {

        }
        internal SuperBrowserException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.SuperBrowser, errorCode, componentType, e)
        {

        }
    }
}
