using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MSHTML;
using SHDocVw;
using System.Threading;
using System.Windows.Forms;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;

namespace II.RPA.DataCapture
{
    class ResizeWebBrowser
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr WindowFromPoint(int xPoint, int yPoint);
        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        public static extern IntPtr ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder strText, int nMaxCount);
        public int _Width;
        public int _Height;
        public IHTMLDocument2 _htmlDoc2;
        public const int SW_SHOWMAXIMIZED = 0x03;
        public ResizeWebBrowser()
        {
            _Width = GetSystemMetrics(0);  //屏幕宽度    
            _Height = GetSystemMetrics(1); //屏幕高度
        }
        public bool ResizeToTopWindow()
        {        
            int nType = GetWebBrowserType();
            if (nType == 1)
            {
                PublicConfig.gformCapture._BrowserType = nType;
                return ResizeIEFrameToTopWindow(0.7);
            }
            else if (nType == 2)
            {
                PublicConfig.gformCapture._BrowserType = nType;
                return ResizeChromeToTopWindow();
            }

            return false;
        }
        public bool ResizeToTopWindowByExec()
        {
            int nType = GetWebBrowserType();
            if (nType == 1)
            {
                return ResizeIEFrameToTopWindow(0.7);
            }
            else if (nType == 2)
            {
                return ResizeChromeToTopWindow();
            }

            return false;
        }
        public bool ResizeToTopWindowMax()
        {
            int nType = GetWebBrowserType();
            if (nType == 1)
            {
                return ResizeIEFrameToTopWindowMax();
            }
            else if (nType == 2)
            {
                return ResizeChromeToTopWindowMax();
            }

            return false;
        }
        public bool ResizeIEFrameToTopWindowMax()
        {
            //double dpi = GetDeviceCapsEX();
            //SetWindowPos(PublicConfig.gIntPtrIEFrame, (IntPtr)(-1), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height + 16.0 * dpi - 40 * dpi), 0x40);
            //SetWindowPos(PublicConfig.gIntPtrIEFrame, (IntPtr)(-2), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height + 16.0 * dpi - 40 * dpi), 0x40);

            ShowWindow(PublicConfig.gIntPtrIEFrame, SW_SHOWMAXIMIZED);

            return true;
        }
        public bool ResizeChromeToTopWindowMax()
        {
            return true;
        }
        public int GetWebBrowserType()
        {
            bool bRet = GetWebBrowser(); 
            if (bRet == true)
            {
                return 1;
            }
            else
            {
                int nX = _Width / 2;
                int nY = _Height / 2;
                IntPtr hTarget = WindowFromPoint(nX, nY);
                StringBuilder szClassName = new StringBuilder(255);
                GetClassName(hTarget, szClassName, 255);
                if (szClassName.ToString().ToLower() == "chrome_renderwidgethosthwnd")
                {
                    return 2;
                }
            }

            return 0;
        }
        public bool ResizeChromeToTopWindow()
        {
            return true;
        }
        public bool ResizeIEFrameToTopWindow(double fBit)
        {
            int nPointx = _Width / 2;
            int nPointy = Convert.ToInt32(((double)_Height) * 0.25);
            IntPtr hWnd = WindowFromPoint(nPointx, nPointy);
            int nIter = 0;
            StringBuilder szClassName = new StringBuilder(255);
            do
            {
                if (nIter > 100)
                    break;

                GetClassName(hWnd, szClassName, 255);
                if (szClassName.ToString().ToLower() == "ieframe" || szClassName.ToString().ToLower() == "360se6_frame")
                {
                    break;
                }

                IntPtr hParent = GetParent(hWnd);
                hWnd = hParent;

                nIter++;
            } while (true);

            if (nIter > 100)
            {
                return false;
            }

            PublicConfig.gIntPtrIEFrame = hWnd;

            double dpi = GetDeviceCapsEX();
            if (szClassName.ToString().ToLower() == "360se6_frame")
            {
                ShowWindow(hWnd, 1);
                SetWindowPos(hWnd, (IntPtr)(-1), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), 
                    Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height * fBit - 12 * dpi), 0x40);
                SetWindowPos(hWnd, (IntPtr)(-2), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), 
                    Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height * fBit - 12 * dpi), 0x40);
            }
            else
            {
                ShowWindow(hWnd, 1);
                SetWindowPos(hWnd, (IntPtr)(-1), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), 
                    Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height * fBit - 4 * dpi), 0x40);
                SetWindowPos(hWnd, (IntPtr)(-2), Convert.ToInt32(-8.0 * dpi), Convert.ToInt32(-8.0 * dpi), 
                    Convert.ToInt32(_Width + 16.0 * dpi), Convert.ToInt32((double)_Height * fBit - 4 * dpi), 0x40);
            }
            return true;
        }
        public double GetDeviceCapsEX()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int zoom = GetDeviceCaps(hdc, 88);
            double dpi = 0;
            switch (zoom)
            {
                case 95:
                case 96:
                case 97:
                    dpi = 1;
                    break;
                case 119:
                case 120:
                case 121:
                    dpi = 1.25;
                    break;
                case 143:
                case 144:
                case 145:
                    dpi = 1.5;
                    break;
                case 167:
                case 168:
                case 169:
                    dpi = 1.75;
                    break;
                case 191:
                case 192:
                case 193:
                    dpi = 2;
                    break;
                case 215:
                case 216:
                case 217:
                    dpi = 2.25;
                    break;
                case 239:
                case 240:
                case 241:
                    dpi = 2.5;
                    break;
                case 263:
                case 264:
                case 265:
                    dpi = 2.75;
                    break;
                case 287:
                case 288:
                case 289:
                    dpi = 3.0;
                    break;
                default:
                    break;
            }

            return dpi;
        }
        public bool GetWebBrowser()
        {
            IHTMLDocument2 htmlDoc2;
            bool bRet = PublicConfig.GetDocument2(out htmlDoc2);
            _htmlDoc2 = htmlDoc2;
            return bRet;
        }
        public bool GetResizedUpIEExecutorDocument()
        {
            PublicConfig.gUpMutexInit.WaitOne();
            PublicConfig.RemoveIEExecuteDocAndListener(3);
            PublicConfig.LogTrace("Document Execute NULL 1");
            PublicConfig.ghtmlUpDoc = null;
            PublicConfig.ghtmlUpDoc2 = null;
            PublicConfig.gdocEventListenerUp = null;  
            PublicConfig.gUpMutexInit.ReleaseMutex();

            bool bRet = GetWebBrowser();
            if (bRet == true)
            {
                PublicConfig.gUpMutexInit.WaitOne();
                PublicConfig.ghtmlUpDoc = _htmlDoc2 as HTMLDocument;
                PublicConfig.ghtmlUpDoc2 = _htmlDoc2 as IHTMLDocument2;
                PublicConfig.gdocEventListenerUp = new DocumentEventListenerUp();
                try
                {
                    PublicConfig.LogTrace(string.Format("Execute addEventListener 2"));
                    PublicConfig.ghtmlUpDoc.addEventListener(PublicConfig.gstrShiZaiEventOut, PublicConfig.gdocEventListenerUp, false);
                    PublicConfig.gUpMutexInit.ReleaseMutex();
                    return true;
                }
                catch(Exception ex)
                {
                    PublicConfig.gUpMutexInit.ReleaseMutex();
                    PublicConfig.ReturnErrorInfo(string.Format("请确保该目标采集页为当前屏幕窗体，并且为IE8及以上内核，刷新网页重试!"), 11);
                }
            }
            else
            {
                PublicConfig.ReturnErrorInfo(string.Format("采集页加载显示有误，请确保该目标采集页为当前屏幕窗体!"), 12);
            }

            return false;
        }
    }
}
