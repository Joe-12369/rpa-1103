using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core
{
    public class CaptureProvider:IDisposable
    {
        private readonly PowerWindowProvider m_PowerWindowProvider;

        public CaptureProvider()
        {
            m_PowerWindowProvider = new PowerWindowProvider();
        }

        public bool RegisterWindow(string windowName)
        {
            if (!m_PowerWindowProvider.Init(windowName))
            {
                return false;
            }
            return true;
        }
        public bool RegisterWindow(IntPtr handle)
        {
            if (!m_PowerWindowProvider.Init(handle))
            {
                return false;
            }
            return true;
        }
        private void UnRegister()
        {
            m_PowerWindowProvider?.Cleanup();
        }
        public bool Capture(out Bitmap aBitmap)
        {
            var ret = m_PowerWindowProvider != null;
            var bitsPtr = ret ? m_PowerWindowProvider.Capture() : IntPtr.Zero;
            aBitmap = Image.FromHbitmap(bitsPtr);
            return ret && !bitsPtr.Equals(IntPtr.Zero);
        }
        public bool Capture(IntPtr aHandle,out Bitmap aBitmap)
        {
            if (!m_PowerWindowProvider.Init(aHandle))
            {
                aBitmap = null;
                return false;
            }
            var ret = m_PowerWindowProvider != null;
            var bitsPtr = ret ? m_PowerWindowProvider.Capture() : IntPtr.Zero;
            aBitmap = Image.FromHbitmap(bitsPtr);
            return ret && !bitsPtr.Equals(IntPtr.Zero);
        }
        public bool SetToCenter(IntPtr aWindowHandle)
        {
            if (m_PowerWindowProvider == null || !m_PowerWindowProvider.Init(aWindowHandle))
            {
                return false;
            }
            return m_PowerWindowProvider.SetWindowPositionToCenter();
        }
        #region 目前不需要
        public bool RefreshWindow(string name)
        {
            var ret = !string.IsNullOrEmpty(name) && m_PowerWindowProvider != null;
            if (ret)
            {
                ret = m_PowerWindowProvider.RefreshWindow();
            }

            return ret;
        }
        public bool ChangeWindowHandle(IntPtr handle)
        {
            return m_PowerWindowProvider!=null &&
                   m_PowerWindowProvider.ChangeWindowHandle(handle);
        }
        public bool ChangeWindowHandle(string windowName)
        {
            return m_PowerWindowProvider != null &&
                   m_PowerWindowProvider.ChangeWindowHandle(windowName);
        }
        public bool GetWindowRect(out Win32Types.Rect winRect)
        {
            var ret = m_PowerWindowProvider!=null;
            winRect = ret ? m_PowerWindowProvider.WindowRect : new Win32Types.Rect();
            return ret;
        }
        public bool GetClientRect(out Win32Types.Rect clientRect)
        {
            var ret = m_PowerWindowProvider!=null;
            clientRect = ret ? m_PowerWindowProvider.ClientRect : new Win32Types.Rect();
            return ret;
        }
        public bool GetBitmapDataSize(out int bmpDataSize)
        {
            var ret = m_PowerWindowProvider != null;
            bmpDataSize = ret ? m_PowerWindowProvider.BitmapDataSize : 0;
            return ret;
        }
        public IntPtr GetBitmapPtr()
        {
            if (m_PowerWindowProvider == null)
            {
                return IntPtr.Zero;
            }

            return m_PowerWindowProvider.BitmapPtr;
        }
        public Win32Types.BitmapInfo GetBitmapInfo()
        {
            if (m_PowerWindowProvider == null)
            {
                return new Win32Types.BitmapInfo();
            }

            return m_PowerWindowProvider.BitmapInfo;
        }
        public bool Capture(out IntPtr bitsPtr)
        {
            var ret = m_PowerWindowProvider!=null;
            bitsPtr = ret ? m_PowerWindowProvider.Capture() : IntPtr.Zero;
            return ret && !bitsPtr.Equals(IntPtr.Zero);
        }
        public bool Capture(out IntPtr bitsPtr, out int bufferSize, out Win32Types.Rect texSize)
        {
            if (m_PowerWindowProvider == null)
            {
                bitsPtr = IntPtr.Zero;
                bufferSize = 0;
                texSize = new Win32Types.Rect();
                return false;
            }
            return m_PowerWindowProvider.Capture(out bitsPtr, out bufferSize, out texSize);
        }
        #endregion
        ~CaptureProvider()
        {
            UnRegister();
        }
        public void Dispose()
        {
            UnRegister();
        }
    }
}
