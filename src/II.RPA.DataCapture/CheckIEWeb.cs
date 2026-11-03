using MSHTML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.DataCapture
{
    //Test 123456
    class CheckIEWeb
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        public void CheckExistIETab()
        {
            int nWidth = GetSystemMetrics(0);  //屏幕宽度    
            int nHeight = GetSystemMetrics(1); //屏幕高度

            int nPointx = nWidth / 2;
            int nPointy = Convert.ToInt32(((double)nHeight) * 0.25);
            Task.Run(() =>
            {
 
                int nIterRet = 0;
                do
                {   
                    bool bRet = false;
                    IntPtr hWnd = WindowFromPoint(nPointx, nPointy);
                    int nIter = 0;
                    StringBuilder szClassName = new StringBuilder(255);
                    do
                    {
                        if (nIter > 100)
                            break;

                        GetClassName(hWnd, szClassName, 255);
                        if (szClassName.ToString().ToLower() == "ieframe")
                        {
                            bRet = true;
                            nIterRet = 0;
                            break;
                        }

                        IntPtr hParent = GetParent(hWnd);
                        hWnd = hParent;

                        nIter++;
                    } while (true);

                    if (bRet == false)
                    {
                        nIterRet++;
                        if (nIterRet >= 6)
                        {
                            if (PublicConfig.gformCapture._typeOperation == 1 && PublicConfig.gbRulesGet == true)
                            {
                                nIterRet = 0;
                                PublicConfig.LogTrace(string.Format("请将采集页置于屏幕上,并且点击配置页的保存配置按钮!"));
                                MessageBox.Show(string.Format("请将采集页置于屏幕上,并且点击配置页的保存配置按钮!"), string.Format("警告"), 
                                    MessageBoxButtons.OK, MessageBoxIcon.None,
                                    MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            }
                            else
                            {
                                PublicConfig.ReturnErrorInfo(string.Format("采集页加载显示有误，请确保目标采集页为当前屏幕窗体!"), 19);
                                break;
                            }
                        }
                    }

                    Thread.Sleep(500);
                } while (PublicConfig.gbExecJS);

                PublicConfig.LogTrace(string.Format("CheckExistIETab Exist"));
            });
        }
    }
}
