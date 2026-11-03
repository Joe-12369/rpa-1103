using II.RPA.Plugin.GUI.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Utils
{
    internal class MemoryCacheManager
    {
        public object TempLock = new object();
        public System.Drawing.Point LastPoint = new System.Drawing.Point(0, 0);
        public IntPtr LastIntPtr = IntPtr.Zero;

        public bool IsSameAsLastOne(System.Drawing.Point point, IntPtr intPtr)
        {
            lock (TempLock)
            {
                if (intPtr == LastIntPtr && PointDiffrence.IsSamePoint(LastPoint, point))
                {
                    return true;
                }
                else
                {
                    LastIntPtr = intPtr;
                    LastPoint = point;
                    return false;
                }
            }
        }

        MemoryCache CaptureElementCache = MemoryCache.Default; //key为HndList的最后一个元素 IntPtr
        public void init()
        {
            CaptureElementCache = new MemoryCache("CaptureElementCache");
        }

        public void AddElementToCache(IntPtr intPtr, CapturedElement capturedElement)
        {
            //// 在缓存中设置值
            CaptureElementCache.Set(intPtr.ToString(), capturedElement, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(3) });
        }

        public bool GetElementFromCache(IntPtr intPtr, out CapturedElement capturedElement)
        {
            capturedElement = new CapturedElement();
            var obj = CaptureElementCache.Get(intPtr.ToString());
            if (obj != null)
            {
                capturedElement = (CapturedElement)CaptureElementCache.Get(intPtr.ToString());
                return true;
            }
            return false;
        }


    }
    
    internal class CapturedElement
    {
        public System.Drawing.Point CursorPoint = new System.Drawing.Point(0,0);
        public IGuiNode_6_6_0 PointElement;
        public CapturedElement() { }

        public CapturedElement(System.Drawing.Point point, IGuiNode_6_6_0 pointElement)
        {
            CursorPoint = point;
            PointElement = pointElement;
        }
    }
}
