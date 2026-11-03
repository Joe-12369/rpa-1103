using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CommonDLLBase.Utils
{
    public class WebBrowserVerify
    {
        private static readonly List<eGuiType> eGuiTypeList = new List<eGuiType> { eGuiType.Chrome, eGuiType.Edge, eGuiType.FireFox, eGuiType.Sogou, eGuiType.Web360se };
        public static List<eGuiType> GetWebGuiTypes()
        {
            List<eGuiType> guiTypeList = new List<eGuiType>();
            foreach (var item in eGuiTypeList)
            {
                switch (item)
                {
                    case eGuiType.Edge:
                        if (Process.GetProcessesByName("msedge").Length > 0)
                        {
                            guiTypeList.Add(eGuiType.Edge);
                        }
                        break;
                    case eGuiType.Web360se:
                        if (Process.GetProcessesByName("360se").Length > 0 || Process.GetProcessesByName("360chrome").Length > 0)
                        {
                            guiTypeList.Add(eGuiType.Web360se);
                        }
                        break;
                    case eGuiType.Chrome:
                        if (Process.GetProcessesByName("chrome").Length > 0)
                        {
                            guiTypeList.Add(eGuiType.Chrome);
                        }
                        break;
                    //case eGuiType.Sogou:
                    //    if (Process.GetProcessesByName("搜狗浏览器").Length > 0)
                    //    {
                    //        guiTypeList.Add(eGuiType.Sogou);
                    //    }
                    //    break;
                    case eGuiType.FireFox:
                        if (Process.GetProcessesByName("firefox").Length > 0)
                        {
                            guiTypeList.Add(eGuiType.FireFox);
                        }
                        break;
                    default:
                        break;
                }
            }

            return guiTypeList;
        }
    }
}
